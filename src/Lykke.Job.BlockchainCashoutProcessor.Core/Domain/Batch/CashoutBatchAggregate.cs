using System;
using System.Collections.Generic;
using System.Linq;

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
            string hotWalletAddress,
            DateTime statedAt)
        {
            return new CashoutBatchAggregate(
                state: CashoutBatchState.Started,
                startedAt: statedAt, 
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

        public bool OnBatchStarted()
        {
            return State == CashoutBatchState.Started;
        }

        public bool OnBatchClosed(IEnumerable<(Guid operationId, decimal amount, string destinationAddress)> operationsInBatch)
        {
            var result = State == CashoutBatchState.Closed;

            ClosedAt = DateTime.UtcNow;
            State = CashoutBatchState.Closed;
            ToOperations = operationsInBatch.ToArray();

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
