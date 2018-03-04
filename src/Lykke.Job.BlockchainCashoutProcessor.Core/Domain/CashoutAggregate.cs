﻿using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain
{
    public class CashoutAggregate
    {
        public string Version { get; }

        public CashoutState State { get; private set; }
        public CashoutResult Result { get; private set; }

        public DateTime StartMoment { get; }
        public DateTime? OperationFinishMoment { get; private set; }
        public DateTime? ClientOperationFinishRegistrationMoment { get; private set; }

        public Guid OperationId { get; }
        public Guid ClientId { get; }
        public string BlockchainType { get; }
        public string BlockchainAssetId { get; }
        public string HotWalletAddress { get; }
        public string ToAddress { get; }
        public decimal Amount { get; }
        public string AssetId { get; }

        public string TransactionHash { get; private set; }
        public decimal? TransactionAmount { get; private set; }
        public decimal? Fee { get; private set; }
        public string Error { get; private set; }

        private CashoutAggregate(
            Guid operationId, 
            Guid clientId, 
            string blockchainType, 
            string blockchainAssetId, 
            string hotWalletAddress, 
            string toAddress, 
            decimal amount, 
            string assetId,
            DateTime? enrollmentDate = null,
            Guid? toClientId = null,
            CashoutState state = CashoutState.Started,
            Guid? crossClientOperationId = null)
        {
            StartMoment = DateTime.UtcNow;

            OperationId = operationId;
            ClientId = clientId;
            BlockchainType = blockchainType;
            BlockchainAssetId = blockchainAssetId;
            HotWalletAddress = hotWalletAddress;
            ToAddress = toAddress;
            Amount = amount;
            AssetId = assetId;

            State = state;
            Result = CashoutResult.Unknown;
        }

        private CashoutAggregate(
            string version,
            CashoutState state,
            CashoutResult result,
            DateTime startMoment,
            DateTime? operationFinishMoment,
            DateTime? clientOperationFinishRegistrationMoment,
            Guid operationId,
            Guid clientId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            string assetId,
            string transactionHash,
            decimal? transactionAmount,
            decimal? fee,
            string error)
        {
            Version = version;
            State = state;
            Result = result;

            StartMoment = startMoment;
            OperationFinishMoment = operationFinishMoment;
            ClientOperationFinishRegistrationMoment = clientOperationFinishRegistrationMoment;

            OperationId = operationId;
            ClientId = clientId;
            BlockchainType = blockchainType;
            BlockchainAssetId = blockchainAssetId;
            HotWalletAddress = hotWalletAddress;
            ToAddress = toAddress;
            Amount = amount;
            AssetId = assetId;

            TransactionHash = transactionHash;
            TransactionAmount = transactionAmount;
            Fee = fee;
            Error = error;
        }

        public static CashoutAggregate StartNew(
            Guid operationId,
            Guid clientId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress, 
            string toAddress, 
            decimal amount, 
            string assetId)
        {
            return new CashoutAggregate(
                operationId,
                clientId,
                blockchainType,
                blockchainAssetId,
                hotWalletAddress, 
                toAddress, 
                amount, 
                assetId);
        }

         public static CashoutAggregate StartNewCrossClient(
            Guid operationId,
            Guid clientId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            string assetId)
        {
            return new CashoutAggregate(
                operationId,
                clientId,
                blockchainType,
                blockchainAssetId,
                hotWalletAddress,
                toAddress,
                amount,
                assetId);
        }

        public static CashoutAggregate Restore(
            string version,
            CashoutState state,
            CashoutResult result,
            DateTime startMoment,
            DateTime? operationFinishMoment,
            DateTime? clientOperationFinishRegistrationMoment,
            Guid operationId,
            Guid clientId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            string assetId,
            string transactionHash,
            decimal? transactionAmount,
            decimal? fee,
            string error)
        {
            return new CashoutAggregate(
                version,
                state,
                result,
                startMoment,
                operationFinishMoment,
                clientOperationFinishRegistrationMoment,
                operationId,
                clientId,
                blockchainType,
                blockchainAssetId,
                hotWalletAddress,
                toAddress,
                amount,
                assetId,
                transactionHash,
                transactionAmount,
                fee,
                error
                );
        }

        public bool OnOperationCompleted(string transactionHash, decimal transactionAmount, decimal fee)
        {
            if (!SwitchState(CashoutState.Started, CashoutState.OperationIsFinished))
            {
                return false;
            }

            OperationFinishMoment = DateTime.UtcNow;

            TransactionHash = transactionHash;
            TransactionAmount = transactionAmount;
            Fee = fee;

            Result = CashoutResult.Success;

            return true;
        }

        public bool OnOperationFailed(string error)
        {
            if (!SwitchState(CashoutState.Started, CashoutState.OperationIsFinished))
            {
                return false;
            }

            OperationFinishMoment = DateTime.UtcNow;

            Error = error;

            Result = CashoutResult.Failure;

            return true;
        }

        public bool OnClientOperationFinishRegisteredEvent()
        {
            if (!SwitchState(CashoutState.OperationIsFinished, CashoutState.ClientOperationFinishIsRegistered))
            {
                return false;
            }

            ClientOperationFinishRegistrationMoment = DateTime.UtcNow;

            return true;
        }

        private bool SwitchState(CashoutState expectedState, CashoutState nextState)
        {
            if (State < expectedState)
            {
                // Throws to retry and wait until aggregate will be in the required state
                throw new InvalidAggregateStateException(State, expectedState, nextState);
            }

            if (State > expectedState)
            {
                // Aggregate already in the next state, so this event can be just ignored
                return false;
            }

            State = nextState;

            return true;
        }
    }
}
