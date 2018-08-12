using System;
using System.Linq;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    internal class ActiveBatchEntity : AzureTableEntity
    {
        #region Fields

        public string BlockchainType { get; set; }

        public DateTime StartedAt { get; set; }

        public string BlockchainAssetId { get; set; }

        public string HotWallet { get; set; }

        public Guid BatchId { get; set; }

        [JsonValueSerializer]
        public BatchedCashoutEntity[] Cashouts { get; set; }

        public bool IsSuspended { get; set; }

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
                Cashouts = aggregate.Cashouts
                    .Select(BatchedCashoutEntity.FromDomain)
                    .ToArray(),
                BlockchainType = aggregate.BlockchainType,
                StartedAt = aggregate.StartedAt,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                HotWallet = aggregate.HotWallet,
                IsSuspended =  aggregate.IsSuspended
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
                cashouts: Cashouts
                    .Select(x=>x.ToDomain())
                    .ToHashSet(),
                isSuspended: IsSuspended);
        }
        
        #endregion
    }
}
