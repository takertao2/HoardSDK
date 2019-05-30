﻿using System.Collections.Generic;
using System.ComponentModel;

namespace Plasma.RootChain.Contracts
{
    /// <summary>
    /// Root chain version enum
    /// </summary>
    public enum RootChainVersion
    {
        /// <summary>
        /// v0.1 version
        /// </summary>
        [Description("ari")]
        Ari = 1,
    }

    /// <summary>
    /// Stores root chain's different ABI versions
    /// </summary>
    public static class RootChainABI
    {
        /// <summary>
        /// Current ABI version
        /// </summary>
        public static string CurrentVersion = RootChainVersion.Ari.DescToString();

        /// <summary>
        /// Root chain v0.1 version ABI
        /// </summary>
        public static readonly string AriABI = "{'abi':[{'constant':false,'inputs':[{'name':'_token','type':'address'}],'name':'addToken','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'_inFlightTx','type':'bytes'},{'name':'_inFlightTxInputIndex','type':'uint8'},{'name':'_spendingTx','type':'bytes'},{'name':'_spendingTxInputIndex','type':'uint8'},{'name':'_spendingTxSig','type':'bytes'}],'name':'challengeInFlightExitInputSpent','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'_inFlightTx','type':'bytes'},{'name':'_inFlightTxInputIndex','type':'uint8'},{'name':'_competingTx','type':'bytes'},{'name':'_competingTxInputIndex','type':'uint8'},{'name':'_competingTxId','type':'uint256'},{'name':'_competingTxInclusionProof','type':'bytes'},{'name':'_competingTxSig','type':'bytes'}],'name':'challengeInFlightExitNotCanonical','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'_inFlightTx','type':'bytes'},{'name':'_inFlightTxOutputId','type':'uint256'},{'name':'_inFlightTxInclusionProof','type':'bytes'},{'name':'_spendingTx','type':'bytes'},{'name':'_spendingTxInputIndex','type':'uint256'},{'name':'_spendingTxSig','type':'bytes'}],'name':'challengeInFlightExitOutputSpent','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'_outputId','type':'uint192'},{'name':'_challengeTx','type':'bytes'},{'name':'_inputIndex','type':'uint256'},{'name':'_challengeTxSig','type':'bytes'}],'name':'challengeStandardExit','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'_depositTx','type':'bytes'}],'name':'deposit','outputs':[],'payable':true,'stateMutability':'payable','type':'function'},{'constant':false,'inputs':[{'name':'_depositTx','type':'bytes'}],'name':'depositFrom','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'_inFlightTx','type':'bytes'},{'name':'_outputIndex','type':'uint8'}],'name':'piggybackInFlightExit','outputs':[],'payable':true,'stateMutability':'payable','type':'function'},{'constant':false,'inputs':[{'name':'_token','type':'address'},{'name':'_topUtxoPos','type':'uint256'},{'name':'_exitsToProcess','type':'uint256'}],'name':'processExits','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'_inFlightTx','type':'bytes'},{'name':'_inFlightTxId','type':'uint256'},{'name':'_inFlightTxInclusionProof','type':'bytes'}],'name':'respondToNonCanonicalChallenge','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'_token','type':'address'},{'name':'_amount','type':'uint256'}],'name':'startFeeExit','outputs':[],'payable':true,'stateMutability':'payable','type':'function'},{'constant':false,'inputs':[{'name':'_inFlightTx','type':'bytes'},{'name':'_inputTxs','type':'bytes'},{'name':'_inputTxsInclusionProofs','type':'bytes'},{'name':'_inFlightTxSigs','type':'bytes'}],'name':'startInFlightExit','outputs':[],'payable':true,'stateMutability':'payable','type':'function'},{'constant':false,'inputs':[{'name':'_outputId','type':'uint192'},{'name':'_outputTx','type':'bytes'},{'name':'_outputTxInclusionProof','type':'bytes'}],'name':'startStandardExit','outputs':[],'payable':true,'stateMutability':'payable','type':'function'},{'constant':false,'inputs':[{'name':'_blockRoot','type':'bytes32'}],'name':'submitBlock','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'inputs':[],'payable':false,'stateMutability':'nonpayable','type':'constructor'},{'anonymous':false,'inputs':[{'indexed':false,'name':'blockNumber','type':'uint256'}],'name':'BlockSubmitted','type':'event'},{'anonymous':false,'inputs':[{'indexed':false,'name':'token','type':'address'}],'name':'TokenAdded','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'depositor','type':'address'},{'indexed':true,'name':'blknum','type':'uint256'},{'indexed':true,'name':'token','type':'address'},{'indexed':false,'name':'amount','type':'uint256'}],'name':'DepositCreated','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'owner','type':'address'},{'indexed':false,'name':'outputId','type':'uint256'},{'indexed':false,'name':'amount','type':'uint256'},{'indexed':false,'name':'token','type':'address'}],'name':'ExitStarted','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'initiator','type':'address'},{'indexed':false,'name':'txHash','type':'bytes32'}],'name':'InFlightExitStarted','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'owner','type':'address'},{'indexed':false,'name':'txHash','type':'bytes32'},{'indexed':false,'name':'outputIndex','type':'uint256'}],'name':'InFlightExitPiggybacked','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'challenger','type':'address'},{'indexed':false,'name':'txHash','type':'bytes32'},{'indexed':false,'name':'challengeTxPosition','type':'uint256'}],'name':'InFlightExitChallenged','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'challenger','type':'address'},{'indexed':false,'name':'txHash','type':'bytes32'},{'indexed':false,'name':'outputId','type':'uint256'}],'name':'InFlightExitOutputBlocked','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'utxoPos','type':'uint256'}],'name':'ExitFinalized','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'utxoPos','type':'uint256'}],'name':'ExitChallenged','type':'event'},{'constant':true,'inputs':[{'name':'','type':'uint256'}],'name':'blocks','outputs':[{'name':'root','type':'bytes32'},{'name':'timestamp','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'CHILD_BLOCK_INTERVAL','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_value','type':'uint256'}],'name':'clearFlag','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'pure','type':'function'},{'constant':true,'inputs':[{'name':'','type':'uint192'}],'name':'exits','outputs':[{'name':'owner','type':'address'},{'name':'token','type':'address'},{'name':'amount','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'','type':'address'}],'name':'exitsQueues','outputs':[{'name':'','type':'address'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_value','type':'uint256'}],'name':'flagged','outputs':[{'name':'','type':'bool'}],'payable':false,'stateMutability':'pure','type':'function'},{'constant':true,'inputs':[],'name':'getDepositBlockNumber','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_outputId','type':'uint256'}],'name':'getExitableTimestamp','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_feeExitId','type':'uint256'}],'name':'getFeeExitPriority','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_tx','type':'bytes'}],'name':'getInFlightExitId','outputs':[{'name':'','type':'uint192'}],'payable':false,'stateMutability':'pure','type':'function'},{'constant':true,'inputs':[{'name':'_tx','type':'bytes'},{'name':'_outputIndex','type':'uint256'}],'name':'getInFlightExitOutput','outputs':[{'name':'','type':'address'},{'name':'','type':'address'},{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_outputId','type':'uint256'},{'name':'_tx','type':'bytes'}],'name':'getInFlightExitPriority','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_token','type':'address'}],'name':'getNextExit','outputs':[{'name':'','type':'uint64'},{'name':'','type':'uint192'},{'name':'','type':'bool'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_outputId','type':'uint256'}],'name':'getStandardExitId','outputs':[{'name':'','type':'uint192'}],'payable':false,'stateMutability':'pure','type':'function'},{'constant':true,'inputs':[{'name':'_exitId','type':'uint192'},{'name':'_outputId','type':'uint256'}],'name':'getStandardExitPriority','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_token','type':'address'}],'name':'hasToken','outputs':[{'name':'','type':'bool'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'inFlightExitBond','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'','type':'uint192'}],'name':'inFlightExits','outputs':[{'name':'exitStartTimestamp','type':'uint256'},{'name':'exitMap','type':'uint256'},{'name':'bondOwner','type':'address'},{'name':'oldestCompetitor','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'uniqueId','type':'uint192'}],'name':'isInFlight','outputs':[{'name':'','type':'bool'}],'payable':false,'stateMutability':'pure','type':'function'},{'constant':true,'inputs':[{'name':'exitableTimestamp','type':'uint64'}],'name':'isMature','outputs':[{'name':'','type':'bool'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'priority','type':'uint256'}],'name':'markInFlight','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'pure','type':'function'},{'constant':true,'inputs':[{'name':'priority','type':'uint256'}],'name':'markStandard','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'pure','type':'function'},{'constant':true,'inputs':[],'name':'MIN_EXIT_PERIOD','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'nextChildBlock','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'nextDepositBlock','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'nextFeeExit','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'operator','outputs':[{'name':'','type':'address'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'piggybackBond','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_value','type':'uint256'}],'name':'setFlag','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'pure','type':'function'},{'constant':true,'inputs':[],'name':'standardExitBond','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'}]}";

        /// <summary>
        /// Dictionary storing root chain's ABI
        /// </summary>
        public static readonly Dictionary<string, string> ABIs = new Dictionary<string, string>()
        {
            { RootChainVersion.Ari.DescToString(), AriABI },
        };

        /// <summary>
        /// Gets root chain ABI
        /// </summary>
        /// <param name="version">requested ABI version</param>
        /// <returns></returns>
        public static string GetRootChainABI(string version = null)
        {
            if (version == null)
                version = CurrentVersion;
            if (ABIs.ContainsKey(version))
                return ABIs[version];
            return null;
        }

        private static string DescToString(this RootChainVersion value)
        {
            DescriptionAttribute[] attributes = (DescriptionAttribute[])value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : string.Empty;
        }
    }
}
