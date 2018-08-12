using System;
using System.Linq;
using Common;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    internal class CashoutBatchEntity : AzureTableEntity
    {
        #region Fields

        public DateTime StartedAt { get; set; }

        public DateTime? SuspendedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public Guid BatchId { get; set; }

        public string BlockchainType { get; set; }

        public string BlockchainAssetId { get; set; }

        public bool IncludeFee { get; set; }

        public CashoutBatchState State { get; set; }

        [JsonValueSerializer]
        public BatchedCashoutEntity[] Cashouts { get; set; }

        public string HotWalletAddress { get; set; }

        #endregion
        

        #region Keys

        public static string GetPartitionKey(Guid operationId)
        {
            // Use hash to distribute all records to the different partitions
            var hash = operationId.ToString().CalculateHexHash32(3);

            return $"{hash}";
        }

        public static string GetRowKey(Guid operationId)
        {
            return $"{operationId:D}";
        }

        #endregion


        #region Conversion

        public static CashoutBatchEntity FromDomain(CashoutBatchAggregate aggregate)
        {
            return new CashoutBatchEntity
            {
                ETag = string.IsNullOrEmpty(aggregate.Version) ? "*" : aggregate.Version,
                PartitionKey = GetPartitionKey(aggregate.BatchId),
                RowKey = GetRowKey(aggregate.BatchId),
                State = aggregate.State,
                StartedAt = aggregate.StartedAt,
                SuspendedAt = aggregate.SuspendedAt,
                FinishedAt = aggregate.FinishedAt,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                BlockchainType = aggregate.BlockchainType,
                IncludeFee = aggregate.IncludeFee,
                BatchId = aggregate.BatchId,
                Cashouts = aggregate.Cashouts
                    .Select(BatchedCashoutEntity.FromDomain)
                    .ToArray(),
                HotWalletAddress = aggregate.HotWalletAddress
            };
        }

        public CashoutBatchAggregate ToDomain()
        {
            return CashoutBatchAggregate.Restore(
                ETag,
                State,
                StartedAt,
                SuspendedAt,
                FinishedAt,
                BatchId,
                BlockchainType,
                BlockchainAssetId,
                IncludeFee,
                Cashouts
                    .Select(x => x.ToDomain())
                    .ToArray(),
                HotWalletAddress);
        }

        #endregion
    }
}
