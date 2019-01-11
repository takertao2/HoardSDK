﻿
using System.Threading.Tasks;

namespace Hoard
{
    /// <summary>
    /// Service for managing user accounts. 
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// Create new account for User
        /// </summary>
        /// <param name="user">user to create account for</param>
        /// <returns>new account and also binds to the user</returns>
        Task<AccountInfo> CreateAccount(string name, User user);

        /// <summary>
        /// Load all accounts registered for User
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> RequestAccounts(User user);

        /// <summary>
        /// Sings transaction with account signature
        /// </summary>
        /// <param name="rlpEncodedTransaction"></param>
        /// <param name="signature"></param>
        /// <returns>Signed transaction</returns>
        Task<string> SignTransaction(byte[] rlpEncodedTransaction, AccountInfo signature);

        /// <summary>
        /// Sings any message with account signature
        /// </summary>
        /// <param name="message"></param>
        /// <param name="signature"></param>
        /// <returns>Signed message</returns>
        Task<string> SignMessage(byte[] message, AccountInfo signature);
    }
}
