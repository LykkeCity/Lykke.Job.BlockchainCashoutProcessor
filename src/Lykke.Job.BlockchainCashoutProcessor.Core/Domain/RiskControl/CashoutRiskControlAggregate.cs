using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.RiskControl
{
    public class CashoutRiskControlAggregate
    {
        public string Version { get; }

        public CashoutRiskControlState State { get; private set; }
        public CashoutRiskControlResult Result { get; private set; }

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

        private CashoutRiskControlAggregate(
            string version,
            CashoutRiskControlState state,
            CashoutRiskControlResult result,
            DateTime creationMoment,
            DateTime? startMoment,
            DateTime? operationAcceptanceMoment,
            DateTime? operationRejectionMoment,
            Guid operationId,
            Guid clientId,
            string assetId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            string error)
        {
            Version = version;
            State = state;
            Result = result;

            CreationMoment = creationMoment;
            StartMoment = startMoment;
            OperationAcceptanceMoment = operationAcceptanceMoment;
            OperationRejectionMoment = operationRejectionMoment;

            OperationId = operationId;
            ClientId = clientId;
            AssetId = assetId;
            BlockchainType = blockchainType;
            BlockchainAssetId = blockchainAssetId;
            HotWalletAddress = hotWalletAddress;
            ToAddress = toAddress;
            Amount = amount;

            Error = error;
        }

        public static CashoutRiskControlAggregate Create(
            Guid operationId,
            Guid clientId,
            string assetId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount)
        {
            return new CashoutRiskControlAggregate(
                null,
                CashoutRiskControlState.Created,
                CashoutRiskControlResult.Unknown,
                DateTime.UtcNow,
                null,
                null,
                null,
                operationId,
                clientId,
                assetId,
                blockchainType,
                blockchainAssetId,
                hotWalletAddress,
                toAddress,
                amount,
                null);
        }

        public static CashoutRiskControlAggregate Restore(
            string version,
            CashoutRiskControlState state,
            CashoutRiskControlResult result,
            DateTime creationMoment,
            DateTime? startMoment,
            DateTime? operationAcceptanceMoment,
            DateTime? operationRejectionMoment,
            Guid operationId,
            Guid clientId,
            string assetId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            string error)
        {
            return new CashoutRiskControlAggregate(
                version,
                state,
                result,
                creationMoment,
                startMoment,
                operationAcceptanceMoment,
                operationRejectionMoment,
                operationId,
                clientId,
                assetId,
                blockchainType,
                blockchainAssetId,
                hotWalletAddress,
                toAddress,
                amount,
                error);
        }

        public bool Start()
        {
            if (!SwitchState(CashoutRiskControlState.Created, CashoutRiskControlState.Started))
            {
                return false;
            }

            StartMoment = DateTime.UtcNow;

            return true;
        }

        public bool OnOperationAccepted()
        {
            if (!SwitchState(CashoutRiskControlState.Started, CashoutRiskControlState.OperationAccepted))
            {
                return false;
            }

            OperationAcceptanceMoment = DateTime.UtcNow;

            Result = CashoutRiskControlResult.Success;

            return true;
        }

        public bool OnOperationRejected(string error)
        {
            if (!SwitchState(CashoutRiskControlState.Started, CashoutRiskControlState.OperationRejected))
            {
                return false;
            }

            OperationRejectionMoment = DateTime.UtcNow;

            Result = CashoutRiskControlResult.Failure;

            Error = error;

            return true;
        }

        private bool SwitchState(CashoutRiskControlState expectedState, CashoutRiskControlState nextState)
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
