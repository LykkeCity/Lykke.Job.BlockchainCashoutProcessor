using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.ActiveBatch
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

        public ICollection<(Guid operationId, decimal amount, string destinationAddress)> Operations { get; private set; }

        public static ActiveBatchAggregate StartNew(string blockChainType, 
            string blockchainAssetId, 
            string hotWallet)
        {
            return new ActiveBatchAggregate(blockChainType,
                blockchainAssetId,
                hotWallet,
                batchId:Guid.NewGuid(), 
                version: null,
                startedAt: DateTime.UtcNow, 
                operations:Enumerable.Empty<(Guid operationId, decimal amount, string destinationAddress)>(),
                isSuspended: false);
        }

        public static ActiveBatchAggregate Restore(string blockchainType,
            string blockchainAssetId,
            string hotWallet,
            Guid batchId,
            string version,
            DateTime startedAt,
            IEnumerable<(Guid operationId, decimal amount, string destinationAddress)> operations,
            bool isSuspended)
        {
            return new ActiveBatchAggregate(blockchainType,
                blockchainAssetId,
                hotWallet,
                batchId: batchId,
                version: version,
                startedAt: startedAt,
                operations: operations,
                isSuspended: isSuspended);
        }

        public ActiveBatchAggregate(string blockchainType,
            string blockchainAssetId,
            string hotWallet,
            Guid batchId,
            string version,
            DateTime startedAt,
            IEnumerable<(Guid operationId, decimal amount, string destinationAddress)> operations,
            bool isSuspended)
        {
            BlockchainType = blockchainType;
            BlockchainAssetId = blockchainAssetId;
            HotWallet = hotWallet;
            BatchId = batchId;
            Version = version;
            StartedAt = startedAt;
            Operations = operations.ToList();
            IsSuspended = isSuspended;
        }
        
        public bool NeedToStartBatchExecution(BlockchainCashoutAggregationConfiguration aggregationSettings)
        {
            return  !IsSuspended && (DateTime.UtcNow - StartedAt >= aggregationSettings.MaxPeriod ||
                   Operations.Count >= aggregationSettings.MaxCount);
        }

        public void AddOperation(Guid operationId, string destinationAddress, decimal amount)
        {
            if (IsSuspended)
            {
                throw new ArgumentException("Batch is suspended");
            }
            if (Operations.All(p => p.operationId != operationId))
            {
                Operations.Add((operationId, amount, destinationAddress));
            }
        }

        public void Suspend()
        {
            IsSuspended = true;
        }
    }
}
