﻿using Hoard.BC.Contracts;
using Hoard.BC.Plasma;
using Hoard.Interfaces;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Hoard.BC
{
    /// <summary>
    /// Utility class for child chain (plasma) communication
    /// </summary>
    public class PlasmaComm : IBCComm
    {
        private static string ETH_CURRENCY_ADDRESS = "0x0000000000000000000000000000000000000000";

        private RestClient childChainClient = null;
        private RestClient watcherClient = null;

        private BCComm bcComm = null;

        /// <summary>
        /// Creates PlasmaComm object.
        /// </summary>
        /// <param name="_bcComm">Ethereum blockchain communication</param>
        /// <param name="childChainUrl">childchain rest client</param>
        /// <param name="watcherUrl">watcher rest client</param>
        public PlasmaComm(BCComm _bcComm, string childChainUrl, string watcherUrl)
        {
            bcComm = _bcComm;

            if (Uri.IsWellFormedUriString(childChainUrl, UriKind.Absolute))
            {
                childChainClient = new RestClient(childChainUrl);
                childChainClient.AutomaticDecompression = false;

                //setup a cookie container for automatic cookies handling
                childChainClient.CookieContainer = new System.Net.CookieContainer();
            }

            if (Uri.IsWellFormedUriString(watcherUrl, UriKind.Absolute))
            {
                watcherClient = new RestClient(watcherUrl);
                watcherClient.AutomaticDecompression = false;

                //setup a cookie container for automatic cookies handling
                watcherClient.CookieContainer = new System.Net.CookieContainer();
            }
        }

        /// <inheritdoc/>
        public virtual async Task<Tuple<bool, string>> Connect()
        {
            return await bcComm.Connect();
        }

        /// <inheritdoc/>
        public async Task<BigInteger> GetBalance(HoardID account)
        {
            return await GetBalance(account, ETH_CURRENCY_ADDRESS);
        }

        /// <inheritdoc/>
        public async Task<BigInteger> GetHRDBalance(HoardID account)
        {
            return await GetBalance(account, await bcComm.GetHRDAddress());
        }

        /// <inheritdoc/>
        public async Task<bool> RegisterHoardGame(GameID game)
        {
            return await bcComm.RegisterHoardGame(game);
        }

        /// <inheritdoc/>
        public void UnregisterHoardGame(GameID game)
        {
            bcComm.UnregisterHoardGame(game);
        }

        /// <inheritdoc/>
        public GameID[] GetRegisteredHoardGames()
        {
            return bcComm.GetRegisteredHoardGames();
        }

        /// <inheritdoc/>
        public async Task<GameID[]> GetHoardGames()
        {
            return await bcComm.GetHoardGames();
        }

        /// <inheritdoc/>
        public async Task<bool> GetGameExists(BigInteger gameID)
        {
            return await bcComm.GetGameExists(gameID);
        }

        /// <inheritdoc/>
        public async Task<string> GetHoardExchangeContractAddress()
        {
            return await bcComm.GetHoardExchangeContractAddress();
        }

        /// <summary>
        /// Returns tokens data (balance) of given account
        /// </summary>
        /// <param name="account">account to query</param>
        /// <returns></returns>
        public async Task<List<TokenData>> GetTokensData(HoardID account)
        {
            var data = JObject.Parse(string.Format("{{ \"address\" : \"{0}\" }}", account.ToString()));
            var responseString = await SendRequestPost(watcherClient, "account.get_balance", data);

            if (IsResponseSuccess(responseString))
            {
                return JsonConvert.DeserializeObject<List<TokenData>>(GetResponseData(responseString));
            }

            //TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns tokens data (balance) of given account and currency
        /// </summary>
        /// <param name="account">account to query</param>
        /// <param name="currency">currency to query</param>
        /// <returns></returns>
        public async Task<List<TokenData>> GetTokensData(HoardID account, string currency)
        {
            var data = JObject.Parse(string.Format("{{ \"address\" : \"{0}\", \"currency\" : \"{1}\" }}", account.ToString(), currency));
            var responseString = await SendRequestPost(watcherClient, "account.get_balance", data);

            if (IsResponseSuccess(responseString))
            {
                return JsonConvert.DeserializeObject<List<TokenData>>(GetResponseData(responseString));
            }

            //TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sumbits signed transaction to child chain
        /// </summary>
        /// <param name="signedTransaction">RLP encoded signed transaction</param>
        /// <returns></returns>
        public async Task<bool> SubmitTransaction(string signedTransaction)
        {
            var data = JObject.Parse(string.Format("{{ \"transaction\" : \"{0}\" }}", signedTransaction));
            var responseString = await SendRequestPost(childChainClient, "transaction.submit", data);

            if (IsResponseSuccess(responseString))
            {
                var receipt = JsonConvert.DeserializeObject<TransactionReceipt>(GetResponseData(responseString));

                TransactionData transaction = null;
                transaction = await GetTransaction(receipt.TxHash);
                while (transaction == null)
                {
                    Thread.Sleep(1000);
                    transaction = await GetTransaction(receipt.TxHash);
                }

                return true;
            }

            //TODO no sufficient funds
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns current token state (ERC721)
        /// </summary>
        /// <param name="currency">currency to query</param>
        /// <param name="tokenId">id to query</param>
        /// <returns></returns>
        public async Task<byte[]> GetTokenState(string currency, BigInteger tokenId)
        {
            //TODO not implemented in plasma
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns balance of given account and currency
        /// </summary>
        /// <param name="account">account to query</param>
        /// <param name="currency">currency to query</param>
        /// <returns></returns>
        protected async Task<BigInteger> GetBalance(HoardID account, string currency)
        {
            var balances = await GetTokensData(account);
            var utxoData = balances.FirstOrDefault(x => x.Currency == currency.RemoveHexPrefix());

            if (utxoData != null)
                return utxoData.Amount;
            return 0;
        }

        /// <summary>
        /// Returns UTXOs of given account and currency
        /// </summary>
        /// <param name="account">account to query</param>
        /// <param name="currency">currency to query</param>
        /// <returns></returns>
        public async Task<List<UTXOData>> GetUtxos(HoardID account, string currency)
        {
            var data = JObject.Parse(string.Format("{{ \"address\" : \"{0}\" }}", account.ToString()));
            var responseString = await SendRequestPost(watcherClient, "account.get_utxos", data);

            if (IsResponseSuccess(responseString))
            {
                var result = new List<UTXOData>();

                var jsonUtxos = JsonConvert.DeserializeObject<List<string>>(GetResponseData(responseString));
                foreach (var jsonUtxo in jsonUtxos)
                {
                    var utxo = UTXODataFactory.Deserialize(jsonUtxo);
                    if (utxo.Currency == currency)
                    {
                        result.Add(utxo);
                    }
                }

                return result;
            }

            //TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns transaction data of given transaction hash
        /// </summary>
        /// <param name="txId">transaction hash to query</param>
        /// <returns></returns>
        protected async Task<TransactionData> GetTransaction(BigInteger txId)
        {
            var data = JObject.Parse(string.Format("{{ \"id\" : \"{0}\" }}", txId.ToString("x")));
            var responseString = await SendRequestPost(watcherClient, "transaction.get", data);

            if (IsResponseSuccess(responseString))
            {
                return JsonConvert.DeserializeObject<TransactionData>(GetResponseData(responseString));
            }

            //TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates game item adapter for given game and game item contract
        /// </summary>
        /// <param name="game">game id</param>
        /// <param name="contract">game item contract</param>
        /// <returns></returns>
        public GameItemAdapter GetGameItemAdater(GameID game, GameItemContract contract)
        {
            if (contract is ERC223GameItemContract)
                return (GameItemAdapter)Activator.CreateInstance(typeof(ERC223GameItemAdapter), this, game, contract);
            else if (contract is ERC721GameItemContract)
                return (GameItemAdapter)Activator.CreateInstance(typeof(ERC721GameItemAdapter), this, game, contract);

            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns GameItem contract for given game and of given type
        /// </summary>
        /// <param name="game"></param>
        /// <param name="contractAddress"></param>
        /// <param name="contractType"></param>
        /// <param name="abi">[optional] creates contract with a particular abi</param>
        /// <returns></returns>
        public GameItemContract GetGameItemContract(GameID game, string contractAddress, Type contractType, string abi = "")
        {
            return bcComm.GetGameItemContract(game, contractAddress, contractType, abi);
        }

        /// <summary>
        /// Retrieves all GameItem contract addresses registered for a particular game
        /// </summary>
        /// <param name="game">game to query</param>
        /// <returns></returns>
        public async Task<string[]> GetGameItemContracts(GameID game)
        {
            return await bcComm.GetGameItemContracts(game);
        }

        private async Task<string> SendRequestPost(RestClient client, string method, object data)
        {
            var request = new RestRequest(method, Method.POST);
            request.AddDecompressionMethod(System.Net.DecompressionMethods.None);
            request.AddJsonBody(data);

            var response = await client.ExecuteTaskAsync(request).ConfigureAwait(false); ;

            return response.Content;
        }

        private bool IsResponseSuccess(string responseString)
        {
            if (!string.IsNullOrEmpty(responseString))
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                string success = "false";
                result.TryGetValue("success", out success);
                return (success == "true");
            }
            return false;
        }

        private string GetResponseData(string responseString)
        {
            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            string data = "";
            result.TryGetValue("data", out data);
            return data;
        }

        //protected async Task<List<byte[]>> CreateTransaction(List<UTXOData> inputUtxos, AccountInfo fromAccount, string toAddress, BigInteger amount)
        //{
        //    Debug.Assert(inputUtxos.Count <= 2);

        //     create transaction data
        //    var txData = new List<byte[]>();
        //    for(UInt16 i = 0; i < MAX_INPUTS; ++i)
        //    {
        //        if (i < inputUtxos.Count())
        //        {
        //             cannot mix currencies
        //            Debug.Assert(inputUtxos[0].Currency == inputUtxos[i].Currency);

        //            txData.Add(inputUtxos[i].BlkNum.ToBytesForRLPEncoding());
        //            txData.Add(inputUtxos[i].TxIndex.ToBytesForRLPEncoding());
        //            txData.Add(inputUtxos[i].OIndex.ToBytesForRLPEncoding());
        //        }
        //        else
        //        {
        //            txData.Add(BigInteger.Zero.ToBytesForRLPEncoding());
        //            txData.Add(BigInteger.Zero.ToBytesForRLPEncoding());
        //            txData.Add(BigInteger.Zero.ToBytesForRLPEncoding());
        //        }
        //    }

        //    txData.Add(inputUtxos[0].Currency.HexToByteArray());

        //    txData.Add(toAddress.HexToByteArray());
        //    txData.Add(amount.ToBytesForRLPEncoding());

        //    var sum = new BigInteger(0);
        //    inputUtxos.ForEach(x => sum += x.Amount);
        //    if (sum > amount)
        //    {
        //        txData.Add(fromAccount.ID.ToHexByteArray());
        //        txData.Add((sum - amount).ToBytesForRLPEncoding());
        //    }
        //    else
        //    {
        //        txData.Add("0x0000000000000000000000000000000000000000".HexToByteArray());
        //        txData.Add(BigInteger.Zero.ToBytesForRLPEncoding());
        //    }

        //    return txData;
        //}   
    }
}
