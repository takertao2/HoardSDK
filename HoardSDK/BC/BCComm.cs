using Hoard.BC.Contracts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.NonceServices;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Hoard.BC
{
    /// <summary>
    /// Utility class for Blockchain communication.
    /// Uses Nethereum library.
    /// </summary>
    public class BCComm
    {
        private Web3 web = null;
        private GameCenterContract gameCenter = null;
        private Dictionary<GameID, GameContract> gameContracts = new Dictionary<GameID, GameContract>();

        /// <summary>
        /// Creates BCComm object.
        /// </summary>
        /// <param name="client">JsonRpc client implementation</param>
        /// <param name="gameCenterContract">game center contract address</param>
        public BCComm(Nethereum.JsonRpc.Client.IClient client, string gameCenterContract)
        {
            web = new Web3(client);

            gameCenter = GetContract<GameCenterContract>(gameCenterContract);
        }

        /// <summary>
        /// Connects to blockchain using the JsonRpc client and performs a handshake
        /// </summary>
        /// <returns>a pair of [bool result, string return infromation] received from client</returns>
        public async Task<Tuple<bool,string>> Connect()
        {
            var ver = new Nethereum.RPC.Web3.Web3ClientVersion(web.Client);
            try
            {
                return new Tuple<bool, string>(true, await ver.SendRequestAsync());
            }
            catch(Exception ex)
            {
                Trace.Fail(ex.ToString());
                return new Tuple<bool, string>(false, ex.Message);
            }
        }

        /// <summary>
        /// Returns ETH balance of given account
        /// </summary>
        /// <param name="account">account to query</param>
        /// <returns></returns>
        public async Task<BigInteger> GetETHBalance(HoardID account)
        {
            var ver = new Nethereum.RPC.Eth.EthGetBalance(web.Client);
            return (await ver.SendRequestAsync(account)).Value;
        }

        /// <summary>
        /// Retrieves HRD token address from game center
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetHRDAddress()
        {
            return await gameCenter.GetHoardTokenAddressAsync();
        }

        /// <summary>
        /// Returns HRD balance of given account
        /// </summary>
        /// <param name="account">account to query</param>
        /// <returns></returns>
        public async Task<BigInteger> GetHRDBalance(HoardID account)
        {
            string hrdAddress = await GetHRDAddress();
            if (hrdAddress != null)
            {
                if (hrdAddress.StartsWith("0x"))
                    hrdAddress = hrdAddress.Substring(2);

                HoardTokenContract hrdContract = new HoardTokenContract(web, hrdAddress);
                return await hrdContract.GetBalanceOf(account);
            }
            return new BigInteger(0);
        }

        /// <summary>
        /// Returns GameItem contract for given game and of given type
        /// </summary>
        /// <param name="game"></param>
        /// <param name="contractAddress"></param>
        /// <param name="contractType"></param>
        /// <returns></returns>
        public GameItemContract GetGameItemContract(GameID game, string contractAddress, Type contractType)
        {
            return (GameItemContract)Activator.CreateInstance(contractType, game, web, contractAddress);
        }

        /// <summary>
        /// Helper function to get contract of a prticular type
        /// </summary>
        /// <typeparam name="TContract">type of contract</typeparam>
        /// <param name="contractAddress">address of the contract</param>
        /// <returns></returns>
        public TContract GetContract<TContract>(string contractAddress)
        {
            return (TContract)Activator.CreateInstance(typeof(TContract), web, contractAddress);
        }

        /// <summary>
        /// Retrieves all GameItem contract addresses registered for a particular game
        /// </summary>
        /// <param name="game">game to query</param>
        /// <returns></returns>
        public async Task<string[]> GetGameItemContracts(GameID game)
        {
            if (gameContracts.ContainsKey(game))
            {
                GameContract gameContract = new GameContract(web, gameContracts[game].Address);

                ulong count = await gameContract.GetGameItemContractCountAsync();

                string[] contracts = new string[count];
                for (ulong i = 0; i < count; ++i)
                {
                    BigInteger gameId = await gameContract.GetGameItemIdByIndexAsync(i);
                    contracts[i] = await gameContract.GetGameItemContractAsync(gameId);
                }

                return contracts;
            }

            return null;
        }

        /// <summary>
        /// Registers existing Hoard game. This game must exist on Hoard platform.
        /// This function performs initial setup of game contract.
        /// </summary>
        /// <param name="game">[in/out] game object must contain valid ID. Other fields will be retrieved from platform</param>
        /// <returns></returns>
        public async Task<bool> RegisterHoardGame(GameID game)
        {
            if (gameContracts.ContainsKey(game))
            {
                Trace.TraceWarning("Game already registered!");
                return true;
            }

            string gameAddress = await gameCenter.GetGameContractAsync(game.ID);

            if (gameAddress != Eth.Utils.EMPTY_ADDRESS)
            {
                GameContract gameContract = new GameContract(web, gameAddress);

                string url = await gameContract.GetGameServerURLAsync();

                game.Name = await gameContract.GetName();
                game.GameOwner = await gameContract.GetOwner();
                game.Url = !url.StartsWith("http") ? "http://" + url : url;

                gameContracts.Add(game, gameContract);
                return true;
            }
            Trace.TraceError($"Game is not registered in Hoard Game Center: game = {game.ID}!");
            return false;
        }

        /// <summary>
        /// Removes game from system. Call when you are finished with using that game
        /// </summary>
        /// <param name="game">game to unregister</param>
        public void UnregisterHoardGame(GameID game)
        {
            gameContracts.Remove(game);
        }

        /// <summary>
        /// Returns all registered games (using RegisterHoardGame)
        /// </summary>
        /// <returns></returns>
        public GameID[] GetRegisteredHoardGames()
        {
            GameID[] games = new GameID[gameContracts.Count];
            gameContracts.Keys.CopyTo(games, 0);
            return games;
        }

        /// <summary>
        /// Retrieves all Hoard games registered on the platform.
        /// </summary>
        /// <returns></returns>
        public async Task<GameID[]> GetHoardGames()
        {
            ulong count = await gameCenter.GetGameCount();
            GameID[] games = new GameID[count];
            for (ulong i = 0; i < count; ++i)
            {
                BigInteger gameID = (await gameCenter.GetGameIdByIndexAsync(i));
                string gameAddress = await gameCenter.GetGameContractAsync(gameID);
                GameID game = new GameID(gameID);                
                GameContract gameContract = new GameContract(web, gameAddress);
                string url = await gameContract.GetGameServerURLAsync();
                game.Name = await gameContract.GetName();
                    game.GameOwner = await gameContract.GetOwner();
                game.Url = !url.StartsWith("http") ? "http://" + url : url;
                games[i] = game;
            }
            return games;
        }

        /// <summary>
        /// Checks if game is registered on Hoard Platform
        /// </summary>
        /// <param name="gameID">game ID to check</param>
        /// <returns></returns>
        public async Task<bool> GetGameExists(BigInteger gameID)
        {
            return await gameCenter.GetGameExistsAsync(gameID);
        }

        /// <summary>
        /// Returns address of Hoard exchange contract
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetHoardExchangeContractAddress()
        {
            return await gameCenter.GetExchangeAddressAsync();
        }

        /// <summary>
        /// Returns Hoard exchange contract
        /// </summary>
        /// <returns></returns>
        internal async Task<ExchangeContract> GetHoardExchangeContract()
        {
            string exchangeAddress = await gameCenter.GetExchangeAddressAsync();
            if (exchangeAddress != null)
            {
                if (exchangeAddress.StartsWith("0x"))
                    exchangeAddress = exchangeAddress.Substring(2);

                BigInteger exchangeAddressInt = BigInteger.Parse(exchangeAddress, NumberStyles.AllowHexSpecifier);
                if (!exchangeAddressInt.Equals(0))
                    return new ExchangeContract(web, exchangeAddress);
            }
            return null;
        }

        /// <summary>
        /// Transfer HRD amount to another account
        /// </summary>
        /// <param name="from">sender account</param>
        /// <param name="to">receiver account</param>
        /// <param name="amount">amount to send</param>
        /// <returns>true if transfer was successful, false otherwise</returns>
        public async Task<bool> TransferHRD(AccountInfo from, string to, BigInteger amount)
        {
            string hoardTokenAddress = await gameCenter.GetHoardTokenAddressAsync();
            if (hoardTokenAddress != null)
            {
                if (hoardTokenAddress.StartsWith("0x"))
                    hoardTokenAddress = hoardTokenAddress.Substring(2);

                BigInteger hoardTokenAddressInt = BigInteger.Parse(hoardTokenAddress, NumberStyles.AllowHexSpecifier);
                if (!hoardTokenAddressInt.Equals(0))
                {
                    HoardTokenContract hrdContract = new HoardTokenContract(web, hoardTokenAddress);
                    return await hrdContract.Transfer(from, to, amount);
                }
            }
            Trace.TraceError("Cannot get proper Hoard Token contract!");
            return false;
        }

        /// <summary>
        /// Sets exchange contract address in game center
        /// </summary>
        /// <param name="account">game center owner account</param>
        /// <param name="exchangeAddress">address of Hoard exchange contract</param>
        /// <returns></returns>
        public async Task<TransactionReceipt> SetExchangeContract(AccountInfo account, string exchangeAddress)
        {
            return await gameCenter.SetExchangeAddressAsync(exchangeAddress, account);
        }

        /// <summary>
        /// Sets HRD token contract address in game center
        /// </summary>
        /// <param name="account">game center owner account</param>
        /// <param name="hoardTokenAddress">address of HRD token contract</param>
        /// <returns></returns>
        public async Task<TransactionReceipt> SetHRDAddress(AccountInfo account, string hoardTokenAddress)
        {
            return await gameCenter.SetHoardTokenAddressAsync(hoardTokenAddress, account);
        }

        /// <summary>
        /// Utility to call functions on blockchain signing it by given account
        /// </summary>
        /// <returns>Receipt of called transaction</returns>
        public static async Task<TransactionReceipt> EvaluateOnBC(Web3 web, AccountInfo account, Function function, params object[] functionInput)
        {
            Debug.Assert(account != null);

            HexBigInteger gas = await function.EstimateGasAsync(account.ID, new HexBigInteger(300000), new HexBigInteger(0), functionInput);

            var nonceService = new InMemoryNonceService(account.ID, web.Client);
            BigInteger nonce = await nonceService.GetNextNonceAsync();

            string data = function.GetData(functionInput);
            var trans = new Nethereum.Signer.Transaction(function.ContractAddress, BigInteger.Zero, nonce, BigInteger.Zero, gas.Value, data);
            string encoded = account.SignTransaction(trans.GetRLPEncodedRaw()).Result;
            if (encoded == null)
            {
                Trace.Fail("Could not sign transaction!");
                return null;
            }

            string txId = await web.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + encoded);
            TransactionReceipt receipt = await web.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId);
            while (receipt == null)
            {
                Thread.Sleep(1000);
                receipt = await web.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId);
            }
            return receipt;
        }
    }
}
