using System;
using Common;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories
{
    public class ActiveBatchEntity:TableEntity
    {
        #region Fields

        public string BlockchainType { get; set; }

        public DateTime StartedAt { get; set; }

        public string BlockchainAssetId { get; set; }

        public string HotWallet { get; set; }

        public Guid BatchId { get; set; }

        public string Operations { get; set; }

        public bool IsClosed { get; set; }

        #endregion

        #region Keys
        
        public static string GeneratePartitionKey(string blockchainType)
        {
            return blockchainType;
        }

        public static string GenerateRowKey(string blockchainAssetId, string hotWallet)
        {
            return $"{blockchainAssetId}_{hotWallet}";
        }

        #endregion

        #region Conversion

        public static ActiveBatchEntity FromDomain(ActiveBatchAggregate aggregate)
        {
            return new ActiveBatchEntity
            {
                BatchId = aggregate.BatchId,
                RowKey = GenerateRowKey(aggregate.BlockchainAssetId, aggregate.HotWallet),
                PartitionKey = GeneratePartitionKey(aggregate.BlockchainType),
                ETag = string.IsNullOrEmpty(aggregate.Version) ? "*" : aggregate.Version,
                Operations = aggregate.Operations.ToJson(),
                BlockchainType = aggregate.BlockchainType,
                StartedAt = aggregate.StartedAt,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                HotWallet = aggregate.HotWallet,
                IsClosed =  aggregate.IsClosed
            };
        }

        public ActiveBatchAggregate ToDomain()
        {
            return ActiveBatchAggregate.Restore(blockchainType: BlockchainType,
                blockchainAssetId: BlockchainAssetId,
                hotWallet: HotWallet,
                batchId: BatchId,
                version: ETag,
                startedAt: StartedAt,
                operations: Operations
                    .DeserializeJson<(Guid operationId, decimal amount, string destinationAddress)[]>(),
                isClosed: IsClosed);
        }
        
        #endregion
    }
}
