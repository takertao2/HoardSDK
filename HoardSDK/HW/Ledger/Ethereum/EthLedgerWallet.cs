﻿using Hid.Net;
using System;
using System.Threading.Tasks;

namespace Hoard.HW.Ledger.Ethereum
{
    public class EthLedgerWallet : LedgerWallet
    {
        private class HDWalletAccountInfo : AccountInfo
        {
            private EthLedgerWallet Wallet;

            public HDWalletAccountInfo(string name, HoardID id, EthLedgerWallet wallet)
                :base(name,id)
            {
                Wallet = wallet;
            }

            public override async Task<string> SignMessage(byte[] input)
            {
                return await Wallet.SignMessage(input, this);
            }

            public override async Task<string> SignTransaction(byte[] input)
            {
                return await Wallet.SignTransaction(input, this);
            }

            public override async Task<AccountInfo> Activate(User user)
            {
                return await Wallet.ActivateAccount(user, this);
            }
        }
        private KeyPath keyPath;
        private byte[] derivation;

        public EthLedgerWallet(IHidDevice hidDevice, string derivationPath, uint index = 0) : base(hidDevice, derivationPath)
        {
            keyPath = new KeyPath(derivationPath).Derive(index);
            derivation = keyPath.ToBytes();
        }

        public override async Task<bool> RequestAccounts(User user)
        {
            var output = await SendRequestAsync(EthGetAddress.Request(derivation));
            if(IsSuccess(output.StatusCode))
            {
                var address = new HoardID(EthGetAddress.GetAddress(output.Data));
                user.Accounts.Add(new HDWalletAccountInfo(AccountInfoName, address, this));
                return true;
            }

            return false;
        }

        public override async Task<string> SignTransaction(byte[] rlpEncodedTransaction, AccountInfo accountInfo)
        {
            uint txLength = (uint)rlpEncodedTransaction.Length;
            uint bytesToCopy = Math.Min(0xff - (uint)derivation.Length, txLength);

            var txChunk = new byte[bytesToCopy];
            Array.Copy(rlpEncodedTransaction, 0, txChunk, 0, bytesToCopy);
            var output = await SendRequestAsync(EthSignTransaction.Request(derivation, txChunk, true));

            txLength -= bytesToCopy;
            uint pos = bytesToCopy;
            while (txLength > 0 && IsSuccess(output.StatusCode))
            {
                bytesToCopy = Math.Min(0xff, txLength);
                txChunk = new byte[bytesToCopy];

                Array.Copy(rlpEncodedTransaction, pos, txChunk, 0, bytesToCopy);
                output = await SendRequestAsync(EthSignTransaction.Request(derivation, txChunk, false));

                txLength -= bytesToCopy;
                pos += bytesToCopy;
            }

            if (IsSuccess(output.StatusCode))
            {
                return EthSignTransaction.GetRLPEncoded(output.Data, rlpEncodedTransaction);
            }

            return null;
        }

        public override async Task<string> SignMessage(byte[] message, AccountInfo accountInfo)
        {
            uint msgLength = (uint)message.Length;
            uint bytesToCopy = Math.Min(0xff - (uint)derivation.Length - sizeof(int), msgLength);

            var messageChunk = new byte[bytesToCopy + sizeof(int)];
            msgLength.ToBytes().CopyTo(messageChunk, 0);

            Array.Copy(message, 0, messageChunk, sizeof(int), bytesToCopy);
            var output = await SendRequestAsync(EthSignMessage.Request(derivation, messageChunk, true));

            msgLength -= bytesToCopy;
            uint pos = bytesToCopy;
            while (msgLength > 0 && IsSuccess(output.StatusCode))
            {
                bytesToCopy = Math.Min(0xff, msgLength);
                messageChunk = new byte[bytesToCopy];

                Array.Copy(message, pos, messageChunk, 0, bytesToCopy);
                output = await SendRequestAsync(EthSignMessage.Request(derivation, messageChunk, false));

                msgLength -= bytesToCopy;
                pos += bytesToCopy;
            }

            if (IsSuccess(output.StatusCode))
            {
                return EthSignMessage.GetStringSignature(output.Data);
            }

            return null;
        }

        public override async Task<AccountInfo> ActivateAccount(User user, AccountInfo accountInfo)
        {
            return await Task.Run(() =>
            {
                if (user.Accounts.Contains(accountInfo))
                {
                    return accountInfo;
                }
                return null;
            });
        }
    }
}