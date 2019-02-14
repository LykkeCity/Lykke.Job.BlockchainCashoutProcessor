using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.RiskControl
{
    public class RiskControlAggregate
    {
        public string Version { get; }

        public RiskControlState State { get; private set; }
        public RiskControlResult Result { get; private set; }
        public CashoutErrorCode? ErrorCode { get; private set; }

        public DateTime CreationMoment { get; }
        public DateTime? StartMoment { get; private set; }
        public DateTime? OperationAcceptanceMoment { get; private set; }
        public DateTime? OperationRejectionMoment { get; private set; }

        public Guid OperationId { get; }
        public Guid ClientId { get; }
        public string BlockchainType { get; }
        public string BlockchainAssetId { get; }
        public string HotWalletAddress { get; }
        public string ToAddress { get; }
        public decimal Amount { get; }
        public string AssetId { get; }
        public string Error { get; private set; }

        private RiskControlAggregate(
            Guid operationId,
            Guid clientId,
            string assetId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            RiskControlState state)
        {
            StartMoment = DateTime.UtcNow;

            OperationId = operationId;
            ClientId = clientId;
            AssetId = assetId;
            BlockchainType = blockchainType;
            BlockchainAssetId = blockchainAssetId;
            HotWalletAddress = hotWalletAddress;
            ToAddress = toAddress;
            Amount = amount;

            State = state;
            Result = RiskControlResult.Unknown;
        }

        public static RiskControlAggregate Create(Guid operationId, Guid clientId, string blockchainType, string blockchainAssetId, string fromAddress, string toAddress, decimal amount)
        {
            throw new NotImplementedException();
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
            OperationAcceptanceMoment = operationFinishMoment;

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

        public static RiskControlAggregate Create(
            Guid operationId,
            Guid clientId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            string assetId)
        {
            return new RiskControlAggregate(
                operationId,
                clientId,
                blockchainType,
                blockchainAssetId,
                hotWalletAddress,
                toAddress,
                amount,
                assetId,
                RiskControlState.Created);
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
            return new RiskControlAggregate(
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

        public bool Start()
        {
            if (!SwitchState(RiskControlState.Created, RiskControlState.Started))
            {
                return false;
            }

            StartMoment = DateTime.UtcNow;

            return true;
        }

        public bool OnOperationAccepted()
        {
            if (!SwitchState(RiskControlState.Started, RiskControlState.OperationAccepted))
            {
                return false;
            }

            OperationAcceptanceMoment = DateTime.UtcNow;

            Result = RiskControlResult.Success;

            return true;
        }

        public bool OnOperationRejected(string error)
        {
            if (!SwitchState(RiskControlState.Started, RiskControlState.OperationRejected))
            {
                return false;
            }

            Error = error;

             = DateTime.UtcNow;

            Result = RiskControlResult.Failure;

            return true;
        }

        private bool SwitchState(RiskControlState expectedState, RiskControlState nextState)
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
