using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.CrossClient
{
    public class CrossClientCashoutAggregate
    {
        public string Version { get; }

        public CrossClientCashoutState State { get; private set; }

        public DateTime StartMoment { get; }

        public Guid OperationId { get; }
        public Guid ClientId { get; }
        public string BlockchainType { get; }
        public string BlockchainAssetId { get; }
        public string HotWalletAddress { get; }
        public string ToAddress { get; }
        public decimal Amount { get; }
        public string AssetId { get; }
        public DateTime? MatchingEngineEnrollementMoment { get; private set; }
        public Guid RecipientClientId { get; }
        public Guid CashinOperationId { get; }

        private CrossClientCashoutAggregate(
            string version,
            Guid operationId,
            Guid clientId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            string assetId,
            Guid recipientClientId,
            Guid cashinOperationId)
        {
            StartMoment = DateTime.UtcNow;
            Version = version;
            OperationId = operationId;
            ClientId = clientId;
            BlockchainType = blockchainType;
            BlockchainAssetId = blockchainAssetId;
            HotWalletAddress = hotWalletAddress;
            ToAddress = toAddress;
            Amount = amount;
            AssetId = assetId;

            State = CrossClientCashoutState.Started;
            RecipientClientId = recipientClientId;
            MatchingEngineEnrollementMoment = null;
            RecipientClientId = RecipientClientId;
            CashinOperationId = cashinOperationId;
        }

        private CrossClientCashoutAggregate(
            string version,
            CrossClientCashoutState state,
            DateTime startMoment,
            Guid operationId,
            Guid clientId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            string assetId,
            DateTime? enrollmentDate,
            Guid recipientClientId,
            Guid cashinOperationId)
        {
            Version = version;
            State = state;

            StartMoment = startMoment;

            OperationId = operationId;
            ClientId = clientId;
            BlockchainType = blockchainType;
            BlockchainAssetId = blockchainAssetId;
            HotWalletAddress = hotWalletAddress;
            ToAddress = toAddress;
            Amount = amount;
            AssetId = assetId;
            MatchingEngineEnrollementMoment = enrollmentDate;
            RecipientClientId = recipientClientId;
            CashinOperationId = cashinOperationId;
        }

        public static CrossClientCashoutAggregate Start(
           Guid operationId,
           Guid clientId,
           string blockchainType,
           string blockchainAssetId,
           string hotWalletAddress,
           string toAddress,
           decimal amount,
           string assetId,
           Guid recipientClientId)
        {
            return new CrossClientCashoutAggregate(
                "*",
                operationId,
                clientId,
                blockchainType,
                blockchainAssetId,
                hotWalletAddress,
                toAddress,
                amount,
                assetId,
                recipientClientId,
                Guid.NewGuid());
        }

        public static CrossClientCashoutAggregate Restore(
            string version,
            CrossClientCashoutState state,
            DateTime startMoment,
            Guid operationId,
            Guid clientId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            string assetId,
            DateTime? enrollmentDate,
            Guid recipientClientId,
            Guid cashinOperationId)
        {
            return new CrossClientCashoutAggregate(
                version,
                state,
                startMoment,
                operationId,
                clientId,
                blockchainType,
                blockchainAssetId,
                hotWalletAddress,
                toAddress,
                amount,
                assetId,
                enrollmentDate,
                recipientClientId,
                cashinOperationId);
        }

        public bool OnEnrolledToMatchingEngine(DateTime matchingEngineEnrollementMoment)
        {
            if (!SwitchState(CrossClientCashoutState.Started, CrossClientCashoutState.EnrolledToMatchingEngine))
            {
                return false;
            }

            MatchingEngineEnrollementMoment = matchingEngineEnrollementMoment;

            return true;
        }

        private bool SwitchState(CrossClientCashoutState expectedState, CrossClientCashoutState nextState)
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
