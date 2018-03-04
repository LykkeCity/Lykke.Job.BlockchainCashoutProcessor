using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain
{
    public class CrossClientCashoutAggregate
    {
        public string Version { get; }

        public CrossClientCashoutState State { get; private set; }

        public DateTime StartMoment { get; }
        public DateTime? OperationFinishMoment { get; private set; }

        public Guid OperationId { get; }
        public Guid ClientId { get; }
        public string BlockchainType { get; }
        public string BlockchainAssetId { get; }
        public string HotWalletAddress { get; }
        public string ToAddress { get; }
        public decimal Amount { get; }
        public string AssetId { get; }

        public decimal? Fee { get; private set; }
        public string Error { get; private set; }
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
            Guid cashinOperationId,
            DateTime? enrollmentDate = null,
            CrossClientCashoutState state = CrossClientCashoutState.StartedCrossClient)
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

            State = state;
            ToClientId = toClientId;
            MatchingEngineEnrollementMoment = enrollmentDate;
            ToClientId = ToClientId;
            CashinOperationId = cashinOperationId;
        }

        private CrossClientCashoutAggregate(
            string version,
            CrossClientCashoutState state,
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
            decimal? fee,
            string error,
            DateTime? enrollmentDate,
            Guid toClientId)
        {
            Version = version;
            State = state;

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
            Fee = fee;
            Error = error;
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
            DateTime? operationFinishMoment,
            Guid operationId,
            Guid clientId,
            string blockchainType,
            string blockchainAssetId,
            string hotWalletAddress,
            string toAddress,
            decimal amount,
            string assetId,
            decimal? fee,
            string error,
            DateTime? enrollmentDate,
            Guid toClient,
            Guid cashinOperationId)
        {
            return new CrossClientCashoutAggregate(
                version, 
                operationId, 
                clientId, 
                blockchainType, 
                blockchainAssetId,
                hotWalletAddress, 
                toAddress, 
                amount, 
                assetId, 
                toClient, 
                cashinOperationId,
                enrollmentDate, 
                state
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
