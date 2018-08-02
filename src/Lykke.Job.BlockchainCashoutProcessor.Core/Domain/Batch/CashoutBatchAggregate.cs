using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batch
{
    public class CashoutBatchAggregate
    {
        public string Version { get; }
        public DateTime StartedAt { get; }

        public DateTime? SuspendedAt { get; private set; }

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
            DateTime? suspendedAt,
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
                suspendedAt: null,
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
            string hotWalletAddress,
            DateTime statedAt)
        {
            return new CashoutBatchAggregate(
                state: CashoutBatchState.Started,
                startedAt: statedAt, 
                suspendedAt: null,
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
            DateTime? suspendedAt,
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
            SuspendedAt = suspendedAt;
            FinishedAt = finishedAt;
            State = state;
            HotWalletAddress = hotWalletAddress;
        }

        public bool OnBatchStarted()
        {
            return State == CashoutBatchState.Started;
        }

        public bool OnBatchSuspended(IEnumerable<(Guid operationId, decimal amount, string destinationAddress)> operationsInBatch)
        {
            var result = State == CashoutBatchState.Suspended;

            SuspendedAt = DateTime.UtcNow;
            State = CashoutBatchState.Suspended;
            ToOperations = operationsInBatch.ToArray();

            return result;
        }

        public bool OnBatchCompeted()
        {
            var result = State == CashoutBatchState.Suspended;

            State = CashoutBatchState.Finished;
            FinishedAt = DateTime.UtcNow;

            return result;
        }

        public bool OnBatchFailed()
        {
            var result = State == CashoutBatchState.Suspended;

            State = CashoutBatchState.Finished;
            FinishedAt = DateTime.UtcNow;

            return result;
        }
    }
}
