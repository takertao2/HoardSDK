﻿using Nethereum.Contracts;
using Nethereum.RPC.NonceServices;
using Nethereum.Web3;
using System.Numerics;
using System.Threading.Tasks;

namespace Hoard.BC.Contracts
{
    //TODO: comment it!
    public class ExchangeContract
    {
        public const string ABI = HoardABIConfig.HoardExchangeABI;

        private readonly Web3 web3;
        private Contract contract;

        public string Address { get { return contract.Address; } }

        public ExchangeContract(Web3 web3, string address)
        {
            this.web3 = web3;
            this.contract = web3.Eth.GetContract(ABI, address);
        }

        private Function GetFunctionTrade()
        {
            return contract.GetFunction("trade");
        }

        private Function GetFunctionOrder()
        {
            return contract.GetFunction("order");
        }

        private Function GetFunctionTradeERC721()
        {
            return contract.GetFunction("tradeERC721");
        }

        private Function GetFunctionOrderERC721()
        {
            return contract.GetFunction("orderERC721");
        }

        private Function GetFunctionTestTrade()
        {
            return contract.GetFunction("testTrade");
        }

        private Function GetFunctionTestTradeERC721()
        {
            return contract.GetFunction("testTradeERC721");
        }

        private Function GetFunctionWithdrawToken()
        {
            return contract.GetFunction("withdrawToken");
        }

        private Function GetFunctionWithdrawTokenERC721()
        {
            return contract.GetFunction("withdrawTokenERC721");
        }

        private Function GetFunctionCancelOrder()
        {
            return contract.GetFunction("cancelOrder");
        }

        private Function GetFunctionCancelOrderERC721()
        {
            return contract.GetFunction("cancelOrderERC721");
        }

        public async Task<bool> Order(
            AccountInfo from,
            string tokenGet,
            BigInteger amountGet,
            string tokenGive,
            BigInteger amountGive,
            ulong blockTimeDuration)
        {
            var nonceService = new InMemoryNonceService(from.ID, web3.Client);
            BigInteger nonce = await nonceService.GetNextNonceAsync();
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var expires = blockNumber.Value + blockTimeDuration;

            var function = GetFunctionOrder();
            return await SendTransaction(from, function, tokenGet, amountGet, tokenGive, amountGive, expires, nonce);
        }

        public async Task<bool> OrderERC721(
            AccountInfo from,
            string tokenGet,
            BigInteger amountGet,
            string tokenGive,
            BigInteger tokenId,
            ulong blockTimeDuration)
        {
            var nonceService =  new InMemoryNonceService(from.ID, web3.Client);
            BigInteger nonce = await nonceService.GetNextNonceAsync();
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var expires = blockNumber.Value + blockTimeDuration;

            var function = GetFunctionOrderERC721();
            return await SendTransaction(from, function, tokenGet, amountGet, tokenGive, tokenId, expires, nonce);
        }

        public async Task<bool> Trade(
            AccountInfo from,
            string tokenGet,
            BigInteger amountGet, 
            string tokenGive,
            BigInteger amountGive,
            BigInteger expires,
            BigInteger nonce,
            string orderOwner,
            BigInteger amount)
        {
            var testTradeFun = GetFunctionTestTrade();

            var test = await testTradeFun.CallAsync<bool>(
                tokenGet,
                amountGet,
                tokenGive,
                amountGive,
                expires,
                nonce,
                orderOwner,
                amount,
                from.ID);

            if (!test)
                return false;

            var function = GetFunctionTrade();
            return await SendTransaction(from, function, tokenGet, amountGet, tokenGive, amountGive, expires, nonce, orderOwner, amount);
        }

        public async Task<bool> TradeERC721(
            AccountInfo from,
            string tokenGet,
            BigInteger amountGet,
            string tokenGive,
            BigInteger tokenId,
            BigInteger expires,
            BigInteger nonce,
            string orderOwner,
            BigInteger amount)
        {
            var testTradeFun = GetFunctionTestTradeERC721();

            var test = await testTradeFun.CallAsync<bool>(
                tokenGet,
                amountGet,
                tokenGive,
                tokenId,
                expires,
                nonce,
                orderOwner,
                amount,
                from.ID);

            if (!test)
                return false;

            var function = GetFunctionTradeERC721();
            return await SendTransaction(from, function, tokenGet, amountGet, tokenGive, tokenId, expires, nonce, orderOwner, amount);
        }

        public async Task<bool> Withdraw(AccountInfo from, string tokenAddress, BigInteger value)
        {
            var function = GetFunctionWithdrawToken();
            return await SendTransaction(from, function, tokenAddress, value);
        }

        public async Task<bool> WithdrawERC721(AccountInfo from, string tokenAddress, BigInteger tokenId)
        {
            var function = GetFunctionWithdrawTokenERC721();
            return await SendTransaction(from, function, tokenAddress, tokenId);
        }

        public async Task<bool> CancelOrder(
            AccountInfo from,
            string tokenGet,
            BigInteger amountGet,
            string tokenGive,
            BigInteger amountGive,
            BigInteger expires,
            BigInteger nonce)
        {
            var function = GetFunctionCancelOrder();
            return await SendTransaction(from, function, tokenGet, amountGet, tokenGive, amountGive, expires, nonce);
        }

        public async Task<bool> CancelOrderERC721(
            AccountInfo from,
            string tokenGet,
            BigInteger amountGet,
            string tokenGive,
            BigInteger tokenIdGive,
            BigInteger expires,
            BigInteger nonce)
        {
            var function = GetFunctionCancelOrderERC721();
            return await SendTransaction(from, function, tokenGet, amountGet, tokenGive, tokenIdGive, expires, nonce);
        }

        private async Task<bool> SendTransaction(AccountInfo from, Function function, params object[] functionInput)
        {
            var gas = await function.EstimateGasAsync(
                from.ID,
                new Nethereum.Hex.HexTypes.HexBigInteger(200000),
                new Nethereum.Hex.HexTypes.HexBigInteger(0),
                functionInput);
            gas = new Nethereum.Hex.HexTypes.HexBigInteger(gas.Value * 2);
            var receipt = await function.SendTransactionAndWaitForReceiptAsync(
                from.ID,
                gas,
                new Nethereum.Hex.HexTypes.HexBigInteger(0),
                null,
                functionInput);
            return receipt.Status.Value == 1;
        }
    }
}