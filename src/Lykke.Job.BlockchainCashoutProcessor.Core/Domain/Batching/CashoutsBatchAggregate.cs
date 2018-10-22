using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public class CashoutsBatchAggregate
    {
        public string Version { get; }
        
        public DateTime StartMoment { get; }
        public DateTime? LastCashoutAdditionMoment { get; private set; }
        public DateTime? ExpirationMoment { get; private set; }
        public DateTime? ClosingMoment { get; private set; }
        public DateTime? IdRevocationMoment { get; private set; }
        public DateTime? FinishMoment { get; private set; }

        public Guid BatchId { get; }
        public string BlockchainType { get; }
        public string AssetId { get; }
        public string BlockchainAssetId { get; }
        public string HotWalletAddress { get; }
        public int CountThreshold { get; }
        public TimeSpan AgeThreshold { get; }
        public ISet<BatchedCashoutValueType> Cashouts { get; }
       
        public CashoutsBatchState State { get; private set; }
        public CashoutsBatchClosingReason ClosingReason { get; private set; }

        public bool IsStillFillingUp => State == CashoutsBatchState.FillingUp;
        public bool HaveToBeExpired => DateTime.UtcNow - StartMoment > AgeThreshold;
        
        private CashoutsBatchAggregate(
            Guid batchId,
            DateTime startMoment,
            string blockchainType,
            string assetId,
            string blockchainAssetId,
            string hotWalletAddress,
            int countThreshold,
            TimeSpan ageThreshold,
            string version,
            ISet<BatchedCashoutValueType> cashouts)
        {
            BatchId = batchId;
            StartMoment = startMoment;
            BlockchainType = blockchainType;
            AssetId = assetId;
            BlockchainAssetId = blockchainAssetId;
            HotWalletAddress = hotWalletAddress;
            CountThreshold = countThreshold;
            AgeThreshold = ageThreshold;
            Version = version;
            Cashouts = cashouts;
        }      

        public static CashoutsBatchAggregate Start(
            Guid batchId,
            string blockchainType,
            string assetId,
            string blockchainAssetId,
            string hotWalletAddress,
            int countThreshold,
            TimeSpan ageThreshold)
        {
            return new CashoutsBatchAggregate
            (
                batchId: batchId,
                startMoment: DateTime.UtcNow,
                blockchainType: blockchainType,
                assetId: assetId,
                blockchainAssetId: blockchainAssetId,
                hotWalletAddress: hotWalletAddress,
                countThreshold: countThreshold,
                ageThreshold: ageThreshold,
                version: null,
                cashouts: new HashSet<BatchedCashoutValueType>()
            )
            {
                State = CashoutsBatchState.FillingUp
            };
        }

        public static CashoutsBatchAggregate Restore(
            string version,
            DateTime startMoment,
            DateTime? lastCashoutAdditionMoment,
            DateTime? expirationMoment,
            DateTime? closingMoment,
            DateTime? idRevocationMoment,
            DateTime? finishMoment,
            Guid batchId,
            string blockchainType,
            string assetId,
            string blockchainAssetId,
            string hotWalletAddress,
            int countThreshold,
            TimeSpan ageThreshold,
            ISet<BatchedCashoutValueType> cashouts,
            CashoutsBatchState state,
            CashoutsBatchClosingReason closingReason)
        {
            return new CashoutsBatchAggregate
            (
                batchId: batchId,
                startMoment: startMoment,
                blockchainType: blockchainType,
                assetId: assetId,
                blockchainAssetId: blockchainAssetId,
                hotWalletAddress: hotWalletAddress,
                countThreshold: countThreshold,
                ageThreshold: ageThreshold,
                version: version,
                cashouts: cashouts
            )
            {
                LastCashoutAdditionMoment = lastCashoutAdditionMoment,
                ExpirationMoment = expirationMoment,
                ClosingMoment = closingMoment,
                IdRevocationMoment = idRevocationMoment,
                FinishMoment = finishMoment,
                State = state,
                ClosingReason = closingReason
            };
        }

        public static Guid GetNextId()
        {
            return Guid.NewGuid();
        }

        public TransitionResult AddCashout(Guid cashoutId, Guid clientId, string toAddress, decimal amount)
        {
            switch (SwitchState(CashoutsBatchState.FillingUp, CashoutsBatchState.FillingUp))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            var oldCashoutsCount = Cashouts.Count;

            Cashouts.Add(new BatchedCashoutValueType(cashoutId, clientId, toAddress, amount));

            if (Cashouts.Count >= CountThreshold)
            {
                switch (SwitchState(CashoutsBatchState.FillingUp, CashoutsBatchState.Filled))
                {
                    case TransitionResult.AlreadyInFutureState:
                        return TransitionResult.AlreadyInFutureState;

                    case TransitionResult.AlreadyInTargetState:
                        return TransitionResult.AlreadyInTargetState;
                }
            }
            else
            {
                if (oldCashoutsCount == Cashouts.Count)
                {
                    return TransitionResult.AlreadyInTargetState;
                }    
            }

            LastCashoutAdditionMoment = DateTime.UtcNow;

            return TransitionResult.Switched;
        }

        public TransitionResult Expire()
        {
            switch (SwitchState(CashoutsBatchState.Filled, CashoutsBatchState.Expired))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            ExpirationMoment = DateTime.UtcNow;

            return TransitionResult.Switched;
        }

        public async Task<TransitionResult> CloseAsync(CashoutsBatchClosingReason reason, IClosedBatchedCashoutRepository closedBatchedCashoutRepository)
        {
            var expectedStates = new[]
            {
                CashoutsBatchState.Filled,
                CashoutsBatchState.Expired
            };

            switch (SwitchState(expectedStates, CashoutsBatchState.Closed))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            await closedBatchedCashoutRepository.EnsureClosedAsync(Cashouts.Select(x => x.CashoutId));

            ClosingReason = reason;
            ClosingMoment = DateTime.UtcNow;
            
            return TransitionResult.Switched;
        }

        public async Task<TransitionResult> RevokeIdAsync(IActiveCashoutsBatchIdRepository activeCashoutsBatchIdRepository)
        {
            switch (SwitchState(CashoutsBatchState.Closed, CashoutsBatchState.IdRevoked))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            await activeCashoutsBatchIdRepository.RevokeActiveIdAsync
            (
                BlockchainType,
                BlockchainAssetId,
                HotWalletAddress,
                BatchId
            );

            IdRevocationMoment = DateTime.UtcNow;

            return TransitionResult.Switched;
        }

        public TransitionResult Complete()
        {
            switch (SwitchState(CashoutsBatchState.IdRevoked, CashoutsBatchState.Finished))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            FinishMoment = DateTime.UtcNow;

            return TransitionResult.Switched;
        }

        public TransitionResult Fail()
        {
            switch (SwitchState(CashoutsBatchState.IdRevoked, CashoutsBatchState.Finished))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            FinishMoment = DateTime.UtcNow;

            return TransitionResult.Switched;
        }

        private TransitionResult SwitchState(CashoutsBatchState expectedState, CashoutsBatchState nextState)
        {
            return SwitchState(new[] { expectedState }, nextState);
        }

        private TransitionResult SwitchState(ICollection<CashoutsBatchState> expectedStates, CashoutsBatchState nextState)
        {
            if (expectedStates.Contains(State))
            {
                State = nextState;

                return TransitionResult.Switched;
            }

            if (State < expectedStates.Max())
            {
                // Throws to retry and wait until aggregate will be in the required state
                throw new InvalidAggregateStateException(State, expectedStates, nextState);
            }

            if (State > expectedStates.Min())
            {
                // Aggregate already in the next state, so this event can be just ignored
                return State == nextState
                    ? TransitionResult.AlreadyInTargetState
                    : TransitionResult.AlreadyInFutureState;
            }

            throw new InvalidOperationException("This shouldn't be happened");
        }
    }
}
