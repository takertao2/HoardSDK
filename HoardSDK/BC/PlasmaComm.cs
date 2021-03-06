﻿using Hoard.BC.Contracts;
using Hoard.BC.Plasma;
using Hoard.Interfaces;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Plasma.RootChain.Contracts;
using PlasmaCore;
using PlasmaCore.RPC.OutputData;
using PlasmaCore.Transactions;
using PlasmaCore.UTXO;
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
    /// Utility class for Plasma communication
    /// </summary>
    public class PlasmaComm : IBCComm
    {
        /// <summary>
        /// Zero address (160bits) / default ethereum currency
        /// </summary>
        public static string ZERO_ADDRESS = "0x0000000000000000000000000000000000000000";
        
        private Web3 web3 = null;

        private BCComm bcComm = null;

        private PlasmaAPIService plasmaApiService = null;

        private RootChainContract rootChainContract = null;
        private RootChainVersion rootChainVersion = RootChainVersion.Default;

        private ITransactionEncoder transactionEncoder = null;

        private Dictionary<string, Eth.Utils.TokenType> tokenTypesDict = new Dictionary<string, Eth.Utils.TokenType>();

        /// <summary>
        /// Creates PlasmaComm object.
        /// </summary>
        /// <param name="ethClient">Ethereum jsonRpc client implementation</param>
        /// <param name="gameCenterContract">game center contract address</param>
        /// <param name="watcherClient">watcher client</param>
        /// <param name="childChainClient">child chain client</param>
        /// <param name="rootChainAddress">root chain address</param>
        /// <param name="_rootChainVersion">root chain version</param>
        public PlasmaComm(Nethereum.JsonRpc.Client.IClient ethClient,
                        string gameCenterContract,
                        PlasmaCore.RPC.IClient watcherClient,
                        PlasmaCore.RPC.IClient childChainClient,
                        string rootChainAddress,
                        RootChainVersion _rootChainVersion)
        {
            web3 = new Web3(ethClient);
            bcComm = new BCComm(ethClient, gameCenterContract);

            plasmaApiService = new PlasmaAPIService(watcherClient, childChainClient);
            rootChainVersion = _rootChainVersion;

            if (rootChainAddress == null)
            {
                var statusData = plasmaApiService.GetStatus().Result;
                rootChainAddress = statusData.ContractAddr;
            }

            if (rootChainAddress != null)
            {
                rootChainContract = new RootChainContract(web3, rootChainAddress, rootChainVersion);
            }

            transactionEncoder = TransactionEncoderFactory.Create(rootChainVersion, rootChainAddress);
        }

        /// <inheritdoc/>
        public virtual async Task<string> Connect()
        {
            return await bcComm.Connect();
        }

        /// <inheritdoc/>
        public async Task<BigInteger> GetBalance(HoardID account)
        {
            return await GetBalance(account, ZERO_ADDRESS);
        }

        /// <inheritdoc/>
        public async Task<BigInteger> GetHRDBalance(HoardID account)
        {
            return await GetBalance(account, await bcComm.GetHRDAddress());
        }

        /// <inheritdoc/>
        public async Task RegisterHoardGame(GameID game)
        {
            await bcComm.RegisterHoardGame(game);
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

        /// <inheritdoc/>
        public async Task<ulong> GetHoardGameCount()
        {
            return await bcComm.GetHoardGameCount();
        }

        /// <summary>
        /// Returns tokens data (balance) of given account
        /// </summary>
        /// <param name="account">account to query</param>
        /// <returns></returns>
        public async Task<BalanceData[]> GetBalanceData(HoardID account)
        {
            return await plasmaApiService.GetBalance(account);
        }

        /// <summary>
        /// Returns tokens balance data of given account and currency
        /// </summary>
        /// <param name="account">account to query</param>
        /// <param name="currency">currency to query</param>
        /// <returns></returns>
        public async Task<BalanceData> GetBalanceData(HoardID account, string currency)
        {
            var balance = await plasmaApiService.GetBalance(account);
            return balance.Where(x => x.Currency == currency.EnsureHexPrefix().ToLower()).FirstOrDefault();
        }

        /// <summary>
        /// Sumbits signed transaction to child chain
        /// </summary>
        /// <param name="signedTransaction">RLP encoded signed transaction</param>
        /// <returns></returns>
        public async Task<TransactionDetails> SubmitTransaction(string signedTransaction)
        {
            var receipt = await plasmaApiService.SubmitTransaction(signedTransaction);
            if (receipt != null)
            {
                TransactionDetails transaction = null;

                // timeout
                do
                {
                    Thread.Sleep(1000);
                    transaction = await plasmaApiService.GetTransaction(receipt.TxHash.HexValue);
                } while (transaction == null);

                return transaction;
            }
            return null;
        }

        /// <summary>
        /// Signs and submits transaction to child chain
        /// </summary>
        /// <param name="profileFrom">profile of the sender</param>
        /// <param name="transaction">transaction to submit</param>
        /// <returns></returns>
        public async Task<TransactionDetails> SubmitTransaction(Profile profileFrom, Transaction transaction)
        {
            string signedTransaction = await SignTransaction(profileFrom, transaction);
            return await SubmitTransaction(signedTransaction);
        }

        /// <summary>
        /// Returns current token state (ERC721)
        /// </summary>
        /// <param name="currency">currency to query</param>
        /// <param name="tokenId">id to query</param>
        /// <returns></returns>
        public async Task<byte[]> GetTokenState(string currency, BigInteger tokenId)
        {
            await Task.Yield();
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
            var balances = await GetBalanceData(account);
            var utxoData = balances.FirstOrDefault(x => x.Currency == currency.EnsureHexPrefix().ToLower());

            if (utxoData != null)
            {
                if(utxoData is FCBalanceData)
                    return (utxoData as FCBalanceData).Amount;
                if(utxoData is NFCBalanceData)
                    return (utxoData as NFCBalanceData).TokenIds.Length;
            }
            return 0;
        }

        /// <summary>
        /// Returns all UTXOs of given account
        /// </summary>
        /// <param name="account">account to query</param>
        /// <returns></returns>
        public async Task<UTXOData[]> GetUtxos(HoardID account)
        {
            return await plasmaApiService.GetUtxos(account);
        }

        /// <summary>
        /// Returns UTXOs of given account in given currency
        /// </summary>
        /// <param name="account">account to query</param>
        /// <param name="currency">currency to query</param>
        /// <returns></returns>
        public async Task<UTXOData[]> GetUtxos(HoardID account, string currency)
        {
            var utxos = await GetUtxos(account);
            var result = new List<UTXOData>();
            foreach (var utxo in utxos)
            {
                if (utxo.Currency == currency.EnsureHexPrefix().ToLower())
                {
                    result.Add(utxo);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Returns UTXO of given account, currency and amount
        /// </summary>
        /// <param name="account">account to query</param>
        /// <param name="currency">currency to query</param>
        /// <param name="data">data to query (amount or token id)</param>
        /// <returns></returns>
        public async Task<UTXOData> GetUtxo(HoardID account, string currency, BigInteger data)
        {
            var utxos = await GetUtxos(account);
            foreach (var utxo in utxos)
            {
                if (utxo.Currency == currency.EnsureHexPrefix().ToLower() && utxo.Data == data)
                    return utxo;
            }
            return null;
        }

        /// <summary>
        /// Returns transaction data of given transaction hash
        /// </summary>
        /// <param name="txHash">transaction hash to query</param>
        /// <returns></returns>
        protected async Task<TransactionDetails> GetTransaction(HexBigInteger txHash)
        {
            return await plasmaApiService.GetTransaction(txHash.HexValue);
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
                return (GameItemAdapter)Activator.CreateInstance(typeof(ERC20GameItemAdapter), this, game, contract);
            else if (contract is ERC721GameItemContract)
                return (GameItemAdapter)Activator.CreateInstance(typeof(ERC721GameItemAdapter), this, game, contract);

            throw new NotSupportedException();
        }

        /// <summary>
        /// Helper function to get contract of a prticular type
        /// </summary>
        /// <param name="contractType">type of contract</param>
        /// <param name="contractAddress">address of the contract</param>
        /// <returns></returns>
        public object GetContract(Type contractType, string contractAddress)
        {
            return bcComm.GetContract(contractType, contractAddress);
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

        /// <summary>
        /// Consolidates user's utxos for given currencies and starts standard exit on the root chain for them
        /// This function may take a very long time to finish execution
        /// </summary>
        /// <param name="profileFrom">profile of the sender</param>
        /// <param name="currencies">currencies to exit</param>
        /// <param name="exitBond">exit transaction bond</param>
        /// <param name="tokenSource">cancellation token source</param>
        /// <returns>exit data (null if unsuccessful)</returns>
        public async Task<ExitData[]> StartStandardExit(Profile profileFrom, string[] currencies, BigInteger? exitBond = null, CancellationTokenSource tokenSource = null)
        {
            if (rootChainContract != null)
            {
                List<BigInteger> mergedUtxos = new List<BigInteger>();
                var balanceData = await plasmaApiService.GetBalance(profileFrom.ID);
                var utxoData = await plasmaApiService.GetUtxos(profileFrom.ID);

                Array.ForEach(currencies, x => x = x.EnsureHexPrefix().ToLower());
                foreach (var data in balanceData)
                {
                    if (currencies.Contains(data.Currency))
                    {
                        if (data is FCBalanceData)
                        {
                            var mergedUtxo = await FCConsolidate(profileFrom, data.Currency, null, tokenSource);
                            if (mergedUtxo != null)
                            {
                                mergedUtxos.Add(mergedUtxo.Position);
                            }
                        }
                        else if (data is NFCBalanceData)
                        {
                            var utxos = utxoData.Where(x => x.Currency == data.Currency);
                            foreach(var utxo in utxos)
                            {
                                mergedUtxos.Add(utxo.Position);
                            }
                        }
                        else
                        {
                            // TODO not supported currency
                            throw new NotSupportedException();
                        }
                    }
                }

                return await StartStandardExitBulk(profileFrom, mergedUtxos.ToArray(), exitBond, tokenSource);
            }
            return null;
        }

        /// <summary>
        /// Starts standard exit on the root chain of given utxo
        /// </summary>
        /// <param name="profileFrom">profile of the sender</param>
        /// <param name="utxoPosition">exit utxo position</param>
        /// <param name="exitBond">exit transaction bond</param>
        /// <param name="tokenSource">cancellation token source</param>
        /// <returns>exit data (null if unsuccessful)</returns>
        public async Task<ExitData> StartStandardExit(Profile profileFrom, BigInteger utxoPosition, BigInteger? exitBond = null, CancellationTokenSource tokenSource = null)
        {
            if (rootChainContract != null)
            {
                ExitData exitData = await plasmaApiService.GetExitData(utxoPosition);
                var transaction = await rootChainContract.StartStandardExit(web3, profileFrom.ID, exitData, exitBond);
                string signedTransaction = await SignTransaction(profileFrom, transaction);
                var receipt = await (await SubmitTransactionOnRootChain(web3, signedTransaction)).Wait(tokenSource);
                if (receipt.Status.Value == 1)
                {
                    exitData.ProcessTimestamp = await rootChainContract.GetExitableTimestamp(web3, exitData.Position);
                    return exitData;
                }
            }
            return null;
        }

        /// <summary>
        /// Starts standard exit on the root chain of given utxo
        /// </summary>
        /// <param name="profileFrom">profile of the sender</param>
        /// <param name="utxoPositions">exit utxo position list</param>
        /// <param name="exitBond">exit transaction bond per utxo</param>
        /// <param name="tokenSource">cancellation token source</param>
        /// <returns>list of exit data (or null if unsuccessful) per each utxoPosition given</returns>
        public async Task<ExitData[]> StartStandardExitBulk(Profile profileFrom, BigInteger[] utxoPositions, BigInteger? exitBond = null, CancellationTokenSource tokenSource = null)
        {
            if (rootChainContract != null)
            {
                ExitData[] exitsData = new ExitData[utxoPositions.Length];
                BCTransaction[] txs = new BCTransaction[utxoPositions.Length];
                //submit all transactions
                for (int i = 0; i < utxoPositions.Length; ++i)
                {
                    BigInteger utxoPosition = utxoPositions[i];
                    ExitData exitData = await plasmaApiService.GetExitData(utxoPosition);
                    var transaction = await rootChainContract.StartStandardExit(web3, profileFrom.ID, exitData, exitBond);
                    string signedTransaction = await SignTransaction(profileFrom, transaction);
                    txs[i] = await SubmitTransactionOnRootChain(web3, signedTransaction);
                    exitsData[i] = exitData;
                }

                BigInteger maxUtxoPosition = exitsData.Where(x => x != null).Max(x => x.Position);
                BigInteger processTimestamp = await rootChainContract.GetExitableTimestamp(web3, maxUtxoPosition);

                //now wait for all transaction to finish
                for (int i = 0; i < utxoPositions.Length; ++i)
                {
                    var receipt = await txs[i].Wait(tokenSource);
                    if (receipt.Status.Value != 1)
                    {
                        exitsData[i] = null;
                    }
                    else
                    {
                        exitsData[i].ProcessTimestamp = processTimestamp;
                    }
                }

                return exitsData;
            }
            return null;
        }

        /// <summary>
        /// Processes standard exit on the root chain
        /// </summary>
        /// <param name="profileFrom">profile of the sender</param>
        /// <param name="currency">transaction currency</param>
        /// <param name="topUtxoPosition">starting index of exit</param>
        /// <param name="exitsToProcess">number exits to process</param>
        /// <returns></returns>
        public async Task<BCTransaction> ProcessExits(Profile profileFrom, string currency, BigInteger topUtxoPosition, BigInteger exitsToProcess)
        {
            if (rootChainContract != null)
            {
                var transaction = await rootChainContract.ProcessExits(web3, profileFrom.ID, currency, topUtxoPosition, exitsToProcess);
                string signedTransaction = await SignTransaction(profileFrom, transaction);
                return await SubmitTransactionOnRootChain(web3, signedTransaction);
            }
            return null;
        }

        /// <summary>
        /// Deposits given amount of ether (wei) to the child chain
        /// </summary>
        /// <param name="profileFrom">profile of the sender</param>
        /// <param name="amount">amount of ether (wei) to send</param>
        /// <returns>transaction object</returns>
        public async Task<BCTransaction> Deposit(Profile profileFrom, BigInteger amount)
        {
            if (rootChainContract != null)
            {
                var depositPlasmaTx = new PlasmaCore.Transactions.Transaction();
                depositPlasmaTx.AddOutput(profileFrom.ID, ZERO_ADDRESS, amount);

                RawTransactionEncoder txEncoder = new RawTransactionEncoder();
                byte[] encodedDepositTx = txEncoder.EncodeRaw(depositPlasmaTx);

                var depositTx = await rootChainContract.Deposit(web3, profileFrom.ID, encodedDepositTx, amount);
                string signedDepositTx = await SignTransaction(profileFrom, depositTx);
                return await SubmitTransactionOnRootChain(web3, signedDepositTx);
            }
            return null;
        }

        /// <summary>
        /// Prepare deposit with given tokens to the child chain
        /// </summary>
        /// <param name="profileFrom">profile of the sender</param>
        /// <param name="currency">transaction currency</param>
        /// <param name="data">data to prepare (amount for ERC20, tokenid for ERC721)</param>
        /// <returns></returns>
        public async Task<BCTransaction> PrepareDeposit(Profile profileFrom, string currency, BigInteger data)
        {
            if (rootChainContract != null)
            {
                Nethereum.Signer.Transaction approveTx = null;

                var currencyType = await GetCurrencyType(currency);
                var tokenHandler = web3.Eth.GetContractHandler(currency);
                if(!IsFungibleCurrency(currencyType))
                {
                    // TODO approve one item (ApproveFunction) or all (SetApprovalForAllFunction)?
                    var approveFunc = tokenHandler.GetFunction<Nethereum.StandardNonFungibleTokenERC721.ContractDefinition.ApproveFunction>();

                    var approveInput = new Nethereum.StandardNonFungibleTokenERC721.ContractDefinition.ApproveFunction();
                    approveInput.To = rootChainContract.Address;
                    approveInput.TokenId = data;

                    approveTx = await ContractHelper.CreateTransaction(web3, profileFrom.ID, BigInteger.Zero, approveFunc, approveInput);
                }
                else
                {
                    var approveFunc = tokenHandler.GetFunction<Nethereum.StandardTokenEIP20.ContractDefinition.ApproveFunction>();

                    var approveInput = new Nethereum.StandardTokenEIP20.ContractDefinition.ApproveFunction();
                    approveInput.Spender = rootChainContract.Address;
                    approveInput.Value = data;

                    approveTx = await ContractHelper.CreateTransaction(web3, profileFrom.ID, BigInteger.Zero, approveFunc, approveInput);
                }

                string signedApproveTx = await SignTransaction(profileFrom, approveTx);
                return await SubmitTransactionOnRootChain(web3, signedApproveTx);
            }
            return null;
        }

        /// <summary>
        /// Deposits given token data (amount for ERC20 or tokend id for ERC721) to the child chain assuming PrepareDeposit has been called first.
        /// </summary>
        /// <param name="profileFrom">profile of the sender</param>
        /// <param name="currency">transaction currency</param>
        /// <param name="data">data to send (amount for ERC20 - less than or equal to the amount from PrepareDeposit; tokend id for ERC721)</param>
        /// <returns></returns>
        public async Task<BCTransaction> Deposit(Profile profileFrom, string currency, BigInteger data)
        {
            if (rootChainContract != null && currency != ZERO_ADDRESS)
            {
                var depositPlasmaTx = new PlasmaCore.Transactions.Transaction();
                depositPlasmaTx.AddOutput(profileFrom.ID, currency, data);

                RawTransactionEncoder txEncoder = new RawTransactionEncoder();
                byte[] encodedDepositTx = txEncoder.EncodeRaw(depositPlasmaTx);

                var depositTx = await rootChainContract.DepositToken(web3, profileFrom.ID, encodedDepositTx);
                string signedDepositTx = await SignTransaction(profileFrom, depositTx);
                var depositTrans = await SubmitTransactionOnRootChain(web3, signedDepositTx);
                return depositTrans;
            }        
            return null;
        }

        /// <summary>
        /// Consolidates fungible currency utxo dat into one utxo
        /// </summary>
        /// <param name="profileFrom">profile of the sender</param>
        /// <param name="currency">currency to consolidate</param>
        /// <param name="amount">amount to consolidate (optional, if null consolidate all utxos)</param>
        /// <param name="tokenSource">cancellation token source</param>
        /// <returns></returns>
        public async Task<UTXOData> FCConsolidate(Profile profileFrom, string currency, BigInteger? amount = null, CancellationTokenSource tokenSource = null)
        {
            var currencyType = await GetCurrencyType(currency);
            if (IsFungibleCurrency(currencyType))
            {
                var utxos = await GetUtxos(profileFrom.ID, currency);
                if (utxos.Length > 1)
                {
                    FCConsolidator consolidator = new FCConsolidator(plasmaApiService, transactionEncoder, profileFrom.ID, currency, utxos, amount);
                    while (consolidator.CanMerge)
                    {
                        if (tokenSource != null && tokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        foreach (var transaction in consolidator.Transactions)
                        {
                            await SignTransaction(profileFrom, transaction);
                        }
                        await consolidator.ProcessTransactions();
                    }

                    return consolidator.MergedUtxo;
                }
                else if (utxos.Length == 1)
                {
                    return utxos[0];
                }
            }
            return null;
        }

        /// <summary>
        /// Signs transaction
        /// </summary>
        /// <param name="profile">profile of the signer</param>
        /// <param name="transaction">transaction to sign</param>
        /// <returns>encoded signed transaction</returns>
        public async Task<string> SignTransaction(Profile profile, PlasmaCore.Transactions.Transaction transaction)
        {
            byte[] encodedTx = transactionEncoder.EncodeRaw(transaction);
            string signature = await profile.SignTransaction(encodedTx);
            transaction.SetSignature(profile.ID, signature.HexToByteArray());
            return transactionEncoder.EncodeSigned(transaction).ToHex(true);
        }

        /// <summary>
        /// Signs transaction
        /// </summary>
        /// <param name="profile">profile of the signer</param>
        /// <param name="transactionBody">transaction body (in Omise format) to sign</param>
        /// <returns>encoded signed transaction</returns>
        public async Task<string> SignRawTransaction(Profile profile, byte[] transactionBody)
        {
            var transaction = transactionEncoder.CreateTransaction(transactionBody);
            for (int i = 0; i < transaction.Senders.Length; ++i)
                transaction.Senders[i] = profile.ID;
            var encodedTx = transactionEncoder.EncodeRaw(transaction);
            string signature = await profile.SignTransaction(encodedTx);
            transaction.SetSignature(signature.HexToByteArray());
            return transactionEncoder.EncodeSigned(transaction).ToHex(true);
        }

        /// <summary>
        /// Checks if given exit data is ready to process - first in the process queue and exit timestamp older than current block timestamp.
        /// </summary>
        /// <param name="currency">currency of exit data to check</param>
        /// <param name="exitData">exit data to check</param>
        /// <param name="checkFirstInQueue">if false ignores predecessors check and checks only if enough time has passed. defaults to true</param>
        /// <returns></returns>
        public async Task<bool> IsExitable(string currency, ExitData exitData, bool checkFirstInQueue = true)
        {
            HexBigInteger blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var txs = await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(blockNumber);

            bool canExit = exitData.ProcessTimestamp < txs.Timestamp.Value;

            if (canExit && checkFirstInQueue)
            {
                var exitId = await rootChainContract.GetStandardExitId(web3, rootChainVersion, exitData.Position, exitData.TxBytes.HexToByteArray());
                var nextExitData = await rootChainContract.GetNextExit(web3, currency);
                canExit = nextExitData.ExitId == exitId;
            }

            return canExit;
        }

        /// <summary>
        /// Checks if token was added to plasma network
        /// </summary>
        /// <param name="tokenAddress">token address</param>
        /// <returns></returns>
        public async Task<bool> HasToken(string tokenAddress)
        {
            return await rootChainContract.HasToken(web3, tokenAddress);
        }

        /// <summary>
        /// Adds token to plasma network
        /// </summary>
        /// <param name="profileFrom">profile of the sender</param>
        /// <param name="tokenAddress">token address</param>
        /// <param name="tokenSource">cancellation token source</param>
        /// <returns>receipt of a transaction</returns>
        public async Task<Nethereum.RPC.Eth.DTOs.TransactionReceipt> AddToken(Profile profileFrom, string tokenAddress, CancellationTokenSource tokenSource = null)
        {
            var transaction = await rootChainContract.AddToken(web3, profileFrom.ID, tokenAddress);
            string signedTransaction = await SignTransaction(profileFrom, transaction);
            return await (await SubmitTransactionOnRootChain(web3, signedTransaction)).Wait(tokenSource);
        }

        /// <summary>
        /// Challenges a standard exit
        /// </summary>
        /// <param name="profileFrom">profile of the sender</param>
        /// <param name="utxoPosition">utxo position of exit to challange</param>
        /// <param name="tokenSource">cancellation token source</param>
        /// <returns></returns>
        public async Task<Nethereum.RPC.Eth.DTOs.TransactionReceipt> ChallengeStandardExit(Profile profileFrom, BigInteger utxoPosition, CancellationTokenSource tokenSource = null)
        {
            var challengeData = await plasmaApiService.GetChallengeData(utxoPosition);
            var transaction = await rootChainContract.ChallengeStandardExit(web3, profileFrom.ID, challengeData);
            string signedTransaction = await SignTransaction(profileFrom, transaction);
            return await (await SubmitTransactionOnRootChain(web3, signedTransaction)).Wait(tokenSource);
        }

        private async Task<string> SendRequestPost(RestClient client, string method, object data)
        {
            var request = new RestRequest(method, Method.POST);
            request.AddDecompressionMethod(System.Net.DecompressionMethods.None);
            request.AddParameter("application/json", data.ToString(), ParameterType.RequestBody);
            var response = await client.ExecuteTaskAsync(request).ConfigureAwait(false); ;

            return response.Content;
        }

        private async Task<string> SignTransaction(Profile profile, Nethereum.Signer.Transaction transaction)
        {
            string signature = await profile.SignTransaction(transaction.GetRLPEncodedRaw());
            transaction.SetSignature(Nethereum.Signer.EthECDSASignatureFactory.ExtractECDSASignature(signature));
            return transaction.GetRLPEncoded().ToHex(true);
        }        

        private static async Task<BCTransaction> SubmitTransactionOnRootChain(Web3 web3, string signedTransaction)
        {
            string txId = await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(signedTransaction).ConfigureAwait(false);
            return new BCTransaction(web3, txId);
        }

        /// <summary>
        /// Updates token types
        /// </summary>
        private async Task<Eth.Utils.TokenType> GetCurrencyType(string currency)
        {
            if (!tokenTypesDict.ContainsKey(currency))
            {
                if (currency == ZERO_ADDRESS)
                {
                    tokenTypesDict[currency] = Eth.Utils.TokenType.Ether;
                }
                else
                {
                    var tokenHandler = web3.Eth.GetContractHandler(currency);
                    var supportsFunc = tokenHandler.GetFunction<Nethereum.StandardNonFungibleTokenERC721.ContractDefinition.SupportsInterfaceFunction>();
                    var supportsInput = new Nethereum.StandardNonFungibleTokenERC721.ContractDefinition.SupportsInterfaceFunction();

                    supportsInput.InterfaceId = ERC721GameItemContract.InterfaceID.HexToByteArray();
                    if (await supportsFunc.CallAsync<bool>(supportsInput))
                    {
                        tokenTypesDict[currency] = Eth.Utils.TokenType.ERC721;
                    }
                    else
                    {
                        tokenTypesDict[currency] = Eth.Utils.TokenType.ERC20;
                    }
                }
            }
            return tokenTypesDict[currency];
        }

        private bool IsFungibleCurrency(Eth.Utils.TokenType currencyType)
        {
            return (currencyType == Eth.Utils.TokenType.ERC20 || currencyType == Eth.Utils.TokenType.Ether);
        }
    }
}
