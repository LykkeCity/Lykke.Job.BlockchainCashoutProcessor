using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain
{
    public class ActiveBatchAggregate
    {
        public string BlockchainType { get; }

        public DateTime StartedAt { get; }

        public string BlockchainAssetId { get; }

        public string HotWallet { get; }

        public Guid BatchId { get; }

        public string Version { get; }

        public bool IsClosed { get; private set; }

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
                isClosed: false);
        }

        public static ActiveBatchAggregate Restore(string blockchainType,
            string blockchainAssetId,
            string hotWallet,
            Guid batchId,
            string version,
            DateTime startedAt,
            IEnumerable<(Guid operationId, decimal amount, string destinationAddress)> operations,
            bool isClosed)
        {
            return new ActiveBatchAggregate(blockchainType,
                blockchainAssetId,
                hotWallet,
                batchId: batchId,
                version: version,
                startedAt: startedAt,
                operations: operations,
                isClosed: isClosed);
        }

        public ActiveBatchAggregate(string blockchainType,
            string blockchainAssetId,
            string hotWallet,
            Guid batchId,
            string version,
            DateTime startedAt,
            IEnumerable<(Guid operationId, decimal amount, string destinationAddress)> operations,
            bool isClosed)
        {
            BlockchainType = blockchainType;
            BlockchainAssetId = blockchainAssetId;
            HotWallet = hotWallet;
            BatchId = batchId;
            Version = version;
            StartedAt = startedAt;
            Operations = operations.ToList();
            IsClosed = isClosed;
        }

        public void AddOperation(Guid operationId, string destinationAddress, decimal amount)
        {
            if (Operations.All(p => p.operationId != operationId))
            {
                Operations.Add((operationId, amount, destinationAddress));
            }
        }

        public void Close()
        {
            IsClosed = true;
        }
    }
}
