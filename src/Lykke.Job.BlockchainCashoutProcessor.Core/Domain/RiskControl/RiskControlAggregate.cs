using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.RiskControl
{
    public class RiskControlAggregate
    {
        public string Version { get; }

        public RiskControlState State { get; private set; }
        public RiskControlResult Result { get; private set; }
        public CashoutErrorCode? ErrorCode { get; private set; }

        public DateTime StartMoment { get; }
        public DateTime? OperationFinishMoment { get; private set; }

        public Guid OperationId { get; }

        public Guid? BatchId { get; }
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

        private RiskControlAggregate(
            Guid operationId,
            Guid clientId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            string assetId,
            RiskControlState state)
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

        private RiskControlAggregate(
            string version,
            RiskControlState state,
            RiskControlResult result,
            DateTime startMoment,
            DateTime? operationFinishMoment,
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
            string error,
            Guid? batchId)
        {
            Version = version;
            State = state;
            Result = result;

            StartMoment = startMoment;
            OperationFinishMoment = operationFinishMoment;

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
            BatchId = batchId;
        }

        public static CashoutAggregate Start(
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
                assetId,
                CashoutState.Started);
        }

        public static CashoutAggregate Restore(
            string version,
            CashoutState state,
            CashoutResult result,
            DateTime startMoment,
            DateTime? operationFinishMoment,
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
            string error,
            Guid? batchId)
        {
            return new CashoutAggregate(
                version,
                state,
                result,
                startMoment,
                operationFinishMoment,
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
                error,
                batchId);
        }

        public bool OnOperationCompleted(string transactionHash, decimal transactionAmount, decimal fee, DateTime operationFinishMoment)
        {
            if (!SwitchState(CashoutState.Started, CashoutState.OperationIsFinished))
            {
                return false;
            }

            OperationFinishMoment = operationFinishMoment;

            TransactionHash = transactionHash;
            TransactionAmount = transactionAmount;
            Fee = fee;

            Result = CashoutResult.Success;

            return true;
        }

        public bool OnOperationFailed(string error, CashoutErrorCode? errorCode, DateTime operationFinishMoment)
        {
            if (!SwitchState(CashoutState.Started, CashoutState.OperationIsFinished))
            {
                return false;
            }

            OperationFinishMoment = operationFinishMoment;

            Error = error;

            Result = CashoutResult.Failure;

            ErrorCode = errorCode;

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
