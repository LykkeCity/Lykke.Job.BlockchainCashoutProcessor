using System;
using System.Collections.Generic;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public class ActiveBatchAggregate
    {
        public string BlockchainType { get; }

        public DateTime StartedAt { get; }

        public string BlockchainAssetId { get; }

        public string HotWallet { get; }

        public Guid BatchId { get; }

        public string Version { get; }

        public bool IsSuspended { get; private set; }

        public ISet<BatchedCashoutValueType> Cashouts { get; }

        public static ActiveBatchAggregate StartNew(string blockChainType, 
            string blockchainAssetId, 
            string hotWallet)
        {
            return new ActiveBatchAggregate(blockChainType,
                blockchainAssetId,
                hotWallet,
                batchId: Guid.NewGuid(),
                version: null,
                startedAt: DateTime.UtcNow,
                cashouts: new HashSet<BatchedCashoutValueType>(),
                isSuspended: false);
        }

        public static ActiveBatchAggregate Restore(string blockchainType,
            string blockchainAssetId,
            string hotWallet,
            Guid batchId,
            string version,
            DateTime startedAt,
            ISet<BatchedCashoutValueType> cashouts,
            bool isSuspended)
        {
            return new ActiveBatchAggregate(blockchainType,
                blockchainAssetId,
                hotWallet,
                batchId: batchId,
                version: version,
                startedAt: startedAt,
                cashouts: cashouts,
                isSuspended: isSuspended);
        }

        private ActiveBatchAggregate(string blockchainType,
            string blockchainAssetId,
            string hotWallet,
            Guid batchId,
            string version,
            DateTime startedAt,
            ISet<BatchedCashoutValueType> cashouts,
            bool isSuspended)
        {
            BlockchainType = blockchainType;
            BlockchainAssetId = blockchainAssetId;
            HotWallet = hotWallet;
            BatchId = batchId;
            Version = version;
            StartedAt = startedAt;
            Cashouts = cashouts;
            IsSuspended = isSuspended;
        }
        
        public bool NeedToStartBatchExecution(BlockchainCashoutAggregationConfiguration aggregationSettings)
        {
            return  !IsSuspended && (DateTime.UtcNow - StartedAt >= aggregationSettings.MaxPeriod ||
                   Cashouts.Count >= aggregationSettings.MaxCount);
        }

        public void AddOperation(Guid operationId, string destinationAddress, decimal amount)
        {
            if (IsSuspended)
            {
                throw new ArgumentException("Batch is suspended. Retry after active batch disposed");
            }

            Cashouts.Add(new BatchedCashoutValueType(operationId, destinationAddress, amount));
        }

        public void Suspend()
        {
            IsSuspended = true;
        }
    }
}
