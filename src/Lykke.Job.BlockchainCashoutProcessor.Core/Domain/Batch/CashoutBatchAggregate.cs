using System;
using System.Linq;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batch
{
    public class CashoutBatchAggregate
    {
        public string Version { get; }
        public DateTime StartedAt { get; }

        public DateTime? ClosedAt { get; private set; }

        public DateTime? FinishedAt { get; private set; }

        public Guid BatchId { get; }

        public string BlockchainType { get; }

        public string HotWalletAddress { get; }

        public string BlockchainAssetId { get; }

        public bool IncludeFee { get; }
        
        public CashoutBatchState State { get; private set; }

        public (Guid operationId, decimal amount, string destinationAddress)[] ToOperations { get; private set; }

        public static CashoutBatchAggregate Restore(string version,
            CashoutBatchState state,
            DateTime startedAt,
            DateTime? closedAt,
            DateTime? executedAt,
            Guid batchId,
            string blockchainType,
            string blockchainAssetId,
            bool includeFee,
            (Guid operationId, decimal amount, string destinationAddress)[] operations,
            string hotWalletAddress)
        {
            return new CashoutBatchAggregate(
                state: CashoutBatchState.Started,
                startedAt: startedAt,
                closedAt: null,
                finishedAt: null,
                batchId: batchId,
                blockchainType: blockchainType,
                blockchainAssetId: blockchainAssetId,
                includeFee: includeFee,
                toOperations: operations,
                version: version,
                hotWalletAddress: hotWalletAddress);
        }

        public static CashoutBatchAggregate CreateNew(Guid batchId,
            string blockchainType,
            string blockchainAssetId,
            bool includeFee,
            string hotWalletAddress)
        {
            return new CashoutBatchAggregate(
                state: CashoutBatchState.Started,
                startedAt: DateTime.UtcNow, 
                closedAt: null,
                finishedAt: null,
                batchId: batchId,
                blockchainType: blockchainType,
                blockchainAssetId: blockchainAssetId,
                includeFee: includeFee,
                toOperations: new(Guid operationId, decimal amount,string destinationAddress)[0],
                hotWalletAddress: hotWalletAddress);
        }

        private CashoutBatchAggregate(CashoutBatchState state,
            DateTime startedAt,
            DateTime? closedAt,
            DateTime? finishedAt,
            Guid batchId,
            string blockchainType,
            string blockchainAssetId,
            bool includeFee,
            (Guid operationId, decimal amount, string destinationAddress)[] toOperations,
            string hotWalletAddress,
            string version = null)
        {
            StartedAt = startedAt;
            BatchId = batchId;
            BlockchainAssetId = blockchainAssetId;
            BlockchainType = blockchainType;
            IncludeFee = includeFee;
            ToOperations = toOperations;
            Version = version;
            ClosedAt = closedAt;
            FinishedAt = finishedAt;
            State = state;
            HotWalletAddress = hotWalletAddress;
        }
        
        public bool IsBatchFinished(BlockchainCashoutAggregationConfiguration aggregationConfiguration)
        {
            return DateTime.UtcNow - StartedAt > aggregationConfiguration.MaxPeriod 
                   && ToOperations.Length > aggregationConfiguration.MaxCount;
        }

        public bool OnCashoutBatchStarted()
        {
            return State == CashoutBatchState.Started;
        }
        /// <summary>
        /// return true if aggregate already contains operation or able to add operation 
        /// </summary>
        public bool OnCashoutBatchOperationAdded(Guid operationId, 
            decimal operationAmount,
            string operationDestinationAddress)
        {
            var alreadyContainsOperation = ToOperations.Any(p => p.operationId == operationId);

            if (State != CashoutBatchState.Started)
                return alreadyContainsOperation;

            if (!alreadyContainsOperation)
            {
                var list = ToOperations.ToList();
                list.Add((operationId, operationAmount, operationDestinationAddress));
                ToOperations = list.ToArray();
            }

            return true;
        }

        public bool OnCashoutBatchClosed()
        {
            var result =  State == CashoutBatchState.Closed;

            ClosedAt = DateTime.UtcNow;
            State = CashoutBatchState.Finished;

            return result;
        }

        public bool OnBatchCompeted()
        {
            var result = State == CashoutBatchState.Closed;

            State = CashoutBatchState.Finished;
            FinishedAt = DateTime.UtcNow;

            return result;
        }

        public bool OnBatchFailed()
        {
            var result = State == CashoutBatchState.Closed;

            State = CashoutBatchState.Finished;
            FinishedAt = DateTime.UtcNow;

            return result;
        }
    }
}
