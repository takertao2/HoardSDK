﻿using Hoard;
using Hoard.HW;
using Hoard.HW.Trezor;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RLP;
using Nethereum.Signer;
using System.Threading.Tasks;
using Xunit;

namespace HoardTests.HW
{
    public class TrezorWalletTest
    {
        IAccountService signer;

        public TrezorWalletTest()
        {
            signer = TrezorFactory.GetTrezorWalletAsync(DerivationPath.BIP44).Result;
            Assert.True(signer != null);
        }

        [Fact]
        public async Task DisplayAddress()
        {
            var user = new User("TrezorUser");
            var response = await signer.RequestAccounts(user);
            Assert.True(response);
            Assert.True(user.Accounts.Count > 0);
            Assert.True(user.Accounts[0].Name == TrezorWallet.AccountInfoName);
            Assert.True(user.Accounts[0].ID.Length > 0);
            Assert.True(user.Accounts[0].AccountService is TrezorWallet);
        }

        [Fact]
        public async Task SignTransaction()
        {
            var nonce = 324.ToBytesForRLPEncoding();
            var gasPrice = 10000000000000.ToBytesForRLPEncoding();
            var startGas = 21000.ToBytesForRLPEncoding();
            var to = "0x4bc1EF56d94c766A49153A102096E56fAE2004e1".HexToByteArray();
            var value = 10000.ToBytesForRLPEncoding();
            var data = "".HexToByteArray();

            var txData = new byte[][] { nonce, gasPrice, startGas, to, value, data };
            var tx = new RLPSigner(txData);

            var rlpEncoded = await signer.SignTransaction(tx.GetRLPEncodedRaw(), null);
            Assert.True(rlpEncoded != null);
            Assert.True(rlpEncoded.Length > 0);

            tx = new RLPSigner(rlpEncoded.HexToByteArray(), 6);
            tx.RlpDecode();
            var user = new User("TrezorUser");
            var response = await signer.RequestAccounts(user);
            var account = tx.Key.GetPublicAddress();

            Assert.Equal(user.Accounts[0].ID.ToLower(), account.ToLower());
            Assert.Equal(tx.Data[3].ToHex().ToLower().EnsureHexPrefix(), "0x4bc1EF56d94c766A49153A102096E56fAE2004e1".ToLower());
        }
    }
}
