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
        public Guid ToClientId { get; private set; }
        public Guid CashinOperationId { get; private set; }

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
            Guid toClientId,
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

            State = CrossClientCashoutState.StartedCrossClient;
            ToClientId = toClientId;
            MatchingEngineEnrollementMoment = null;
            ToClientId = ToClientId;
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
            Guid toClientId)
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
            ToClientId = toClientId;
        }

        public static CrossClientCashoutAggregate StartNewCrossClient(
           Guid operationId,
           Guid clientId,
           string blockchainType,
           string blockchainAssetId,
           string hotWalletAddress,
           string toAddress,
           decimal amount,
           string assetId,
           Guid toClientId)
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
                toClientId,
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
            Guid toClientId,
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
                toClientId
                );
        }

        public bool OnEnrolledToMatchingEngine(Guid toClientId)
        {
            if (!SwitchState(CrossClientCashoutState.StartedCrossClient, CrossClientCashoutState.EnrolledToMatchingEngine))
            {
                return false;
            }

            MatchingEngineEnrollementMoment = DateTime.UtcNow;

            ToClientId = toClientId;

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
