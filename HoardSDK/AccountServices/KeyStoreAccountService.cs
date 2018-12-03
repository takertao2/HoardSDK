﻿using Hoard.Utils;
using System;
using System.Threading.Tasks;

namespace Hoard
{
    public class KeyStoreAccountService : IAccountService
    {
        public class KeyStoreAccount : AccountInfo
        {
            public string PrivateKey = null;

            public KeyStoreAccount(string name, string id, string key)
                :base(name,id)
            {
                PrivateKey = key;
            }

            public async override Task<string> SignTransaction(byte[] input)
            {
                return await KeyStoreAccountService.SignTransaction(input,PrivateKey);
            }

            public async override Task<string> SignMessage(byte[] input)
            {
                return await KeyStoreAccountService.SignMessage(input, PrivateKey);
            }
        }

        private HoardServiceOptions Options;

        public KeyStoreAccountService(HoardServiceOptions options)
        {
            Options = options;
        }

        public async Task<AccountInfo> CreateAccount(string name, User user)
        {
            System.Diagnostics.Trace.TraceInformation("Generating user account.");

            string password = await Options.UserInputProvider.RequestInput(user, eUserInputType.kPassword, "new password");

            Tuple<string, string> accountTuple = KeyStoreUtils.CreateAccount(user, name, password);

            AccountInfo accountInfo = new KeyStoreAccount(name, accountTuple.Item1, accountTuple.Item2);
            user.Accounts.Add(accountInfo);

            return accountInfo;
        }

        public Task<bool> RequestAccounts(User user)
        {
            var task = new Task<bool>(() =>
            {
                KeyStoreUtils.EnumerateAccounts(user.UserName, (string accountId) =>
                 {
                     string password = Options.UserInputProvider.RequestInput(user, eUserInputType.kPassword, accountId).Result;
                     Tuple<string, string> accountTuple = KeyStoreUtils.LoadAccount(user.UserName, accountId, password);

                     if (accountTuple != null)
                     {
                         AccountInfo accountInfo = new KeyStoreAccount(accountId, accountTuple.Item1, accountTuple.Item2);
                         user.Accounts.Add(accountInfo);
                     }
                 });
                return user.Accounts.Count > 0;
            });
            task.Start();
            return task;
        }

        public Task<string> SignMessage(byte[] input, AccountInfo signature)
        {
            KeyStoreAccount ksa = signature as KeyStoreAccount;
            if (ksa == null)
            {
                System.Diagnostics.Trace.Fail("Invalid signature!");
                return null;
            }

            return SignMessage(input, ksa.PrivateKey);
        }

        public Task<string> SignTransaction(byte[] input, AccountInfo signature)
        {
            KeyStoreAccount ksa = signature as KeyStoreAccount;
            if (ksa == null)
            {
                System.Diagnostics.Trace.Fail("Invalid signature!");
                return null;
            }

            return SignTransaction(input, ksa.PrivateKey);
        }

        public static Task<string> SignMessage(byte[] input, string privKey)
        {
            var task = new Task<string>(() =>
            {
                var signer = new Nethereum.Signer.EthereumMessageSigner();
                var ecKey = new Nethereum.Signer.EthECKey(privKey);
                return signer.Sign(input, ecKey);
            });
            task.Start();
            return task;
        }

        public static Task<string> SignTransaction(byte[] input, string privKey)
        {
            throw new NotImplementedException();
        }
    }
}
