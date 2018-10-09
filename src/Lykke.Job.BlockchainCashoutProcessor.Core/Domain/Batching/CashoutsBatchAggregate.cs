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
        public DateTime? SuspendMoment { get; private set; }
        public DateTime? FinishMoment { get; private set; }

        public Guid BatchId { get; }
        public string BlockchainType { get; }
        public string AssetId { get; }
        public string BlockchainAssetId { get; }
        public string HotWalletAddress { get; }
        public int CountThreshold { get; }
        public TimeSpan AgeThreshold { get; }
        
        public ISet<BatchedCashoutValueType> Cashouts { get; }
       
        public CashoutBatchState State { get; private set; }
        public string TransactionHash { get; private set; }
        public IReadOnlyCollection<BatchedCashoutValueType> TransactionOutputs { get; private set; }
        public decimal TransactionFee { get; private set; }
        public long TransactionBlock { get; private set; }
        public CashoutBatchClosingReason ClosingReason { get; private set; }

        public bool IsStillFilledUp => State == CashoutBatchState.FillingUp;
        public bool IsExpired => DateTime.UtcNow - StartMoment > AgeThreshold;
        
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

        public static CashoutsBatchAggregate StartNew(
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
                State = CashoutBatchState.FillingUp
            };
        }

        public static CashoutsBatchAggregate Restore(
            string version,
            CashoutBatchState state,
            DateTime startMoment,
            DateTime? suspendToment,
            DateTime? finishMoment,
            Guid batchId,
            string blockchainType,
            string assetId,
            string blockchainAssetId,
            ISet<BatchedCashoutValueType> cashouts,
            string hotWalletAddress,
            int countThreshold,
            TimeSpan ageThreshold,
            string transactionHash,
            IReadOnlyCollection<BatchedCashoutValueType> transactionOutputs,
            decimal transactionFee,
            long transactionBlock,
            CashoutBatchClosingReason closingReason)
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
                State = state,
                SuspendMoment = suspendToment,
                FinishMoment = finishMoment,
                TransactionHash = transactionHash,
                TransactionOutputs = transactionOutputs,
                TransactionFee = transactionFee,
                TransactionBlock = transactionBlock,
                ClosingReason = closingReason
            };
        }

        public static Guid GetNextId()
        {
            return Guid.NewGuid();
        }

        public TransitionResult AddCashout(Guid cashoutId, Guid clientId, string toAddress, decimal amount)
        {
            switch (SwitchState(CashoutBatchState.FillingUp, CashoutBatchState.FillingUp))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            var oldCashoutsCount = Cashouts.Count;

            Cashouts.Add(new BatchedCashoutValueType(cashoutId, clientId, toAddress, amount));

            if (oldCashoutsCount == Cashouts.Count)
            {
                return TransitionResult.AlreadyInTargetState;
            }

            return TransitionResult.Switched;
        }
        
        public TransitionResult Expire()
        {
            switch (SwitchState(CashoutBatchState.FillingUp, CashoutBatchState.Expired))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            return TransitionResult.Switched;
        }

        public TransitionResult Close(CashoutBatchClosingReason reason)
        {
            var expectedStates = new[]
            {
                CashoutBatchState.FillingUp,
                CashoutBatchState.Expired
            };

            switch (SwitchState(expectedStates, CashoutBatchState.Closed))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            ClosingReason = reason;
            
            return TransitionResult.Switched;
        }

        public async Task<TransitionResult> RevokeIdAsync(IActiveCashoutsBatchIdRepository activeCashoutsBatchIdRepository)
        {
            switch (SwitchState(CashoutBatchState.Closed, CashoutBatchState.IdRevoked))
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

            return TransitionResult.Switched;
        }

        public TransitionResult Complete()
        {
            switch (SwitchState(CashoutBatchState.IdRevoked, CashoutBatchState.Finished))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            return TransitionResult.Switched;
        }

        public TransitionResult Fail()
        {
            switch (SwitchState(CashoutBatchState.IdRevoked, CashoutBatchState.Finished))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            return TransitionResult.Switched;
        }

        private TransitionResult SwitchState(CashoutBatchState expectedState, CashoutBatchState nextState)
        {
            return SwitchState(new[] { expectedState }, nextState);
        }

        private TransitionResult SwitchState(ICollection<CashoutBatchState> expectedStates, CashoutBatchState nextState)
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
