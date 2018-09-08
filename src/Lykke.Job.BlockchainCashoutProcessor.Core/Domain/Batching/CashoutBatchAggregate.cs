using System;
using System.Collections.Generic;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
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

        public IReadOnlyCollection<BatchedCashoutValueType> Cashouts { get; private set; }

        public string TransactionHash { get; private set; }

        public IReadOnlyCollection<OperationOutputValueType> TransactionOutputs { get; private set; }

        public decimal TransactionFee { get; private set; }

        public long TransactionBlock { get; private set; }

        
        public static CashoutBatchAggregate Restore(string version,
            CashoutBatchState state,
            DateTime startedAt,
            DateTime? suspendedAt,
            DateTime? finishedAt,
            Guid batchId,
            string blockchainType,
            string blockchainAssetId,
            bool includeFee,
            IReadOnlyCollection<BatchedCashoutValueType> cashouts,
            string hotWalletAddress,
            string transactionHash,
            IReadOnlyCollection<OperationOutputValueType> transactionOutputs,
            decimal transactionFee,
            long transactionBlock)
        {
            return new CashoutBatchAggregate(
                state: state,
                startedAt: startedAt,
                suspendedAt: suspendedAt,
                finishedAt: finishedAt,
                batchId: batchId,
                blockchainType: blockchainType,
                blockchainAssetId: blockchainAssetId,
                includeFee: includeFee,
                cashouts: cashouts,
                version: version,
                hotWalletAddress: hotWalletAddress)
            {
                TransactionHash = transactionHash,
                TransactionOutputs = transactionOutputs,
                TransactionFee = transactionFee,
                TransactionBlock = transactionBlock
            };
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
                cashouts: Array.Empty<BatchedCashoutValueType>(),
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
            IReadOnlyCollection<BatchedCashoutValueType> cashouts,
            string hotWalletAddress,
            string version = null)
        {
            StartedAt = startedAt;
            BatchId = batchId;
            BlockchainAssetId = blockchainAssetId;
            BlockchainType = blockchainType;
            IncludeFee = includeFee;
            Cashouts = cashouts;
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

        public bool OnBatchSuspended(IReadOnlyCollection<BatchedCashoutValueType> cashouts)
        {
            var result = State == CashoutBatchState.Started;

            State = CashoutBatchState.Suspended;

            SuspendedAt = DateTime.UtcNow;

            Cashouts = cashouts;

            return result;
        }

        public bool OnBatchCompeted(
            string transactionHash, 
            IReadOnlyCollection<OperationOutputValueType> transactionOutputs, 
            decimal transactionFee,
            long transactionBlock)
        {
            var result = State == CashoutBatchState.Suspended;

            State = CashoutBatchState.Finished;

            FinishedAt = DateTime.UtcNow;

            TransactionHash = transactionHash;
            TransactionOutputs = transactionOutputs;
            TransactionFee = transactionFee;
            TransactionBlock = transactionBlock;

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
