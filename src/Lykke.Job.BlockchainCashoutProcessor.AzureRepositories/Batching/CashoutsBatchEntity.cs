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
    internal class CashoutsBatchEntity : AzureTableEntity
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

        public string TransactionHash { get; set; }

        public OperationOutputEntity[] TransactionOutputs { get; set; }

        public decimal TransactionFee { get; set; }

        public long TransactionBlock { get; set; }

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

        public static CashoutsBatchEntity FromDomain(CashoutsBatchAggregate aggregate)
        {
            return new CashoutsBatchEntity
            {
                ETag = string.IsNullOrEmpty(aggregate.Version) ? "*" : aggregate.Version,
                PartitionKey = GetPartitionKey(aggregate.BatchId),
                RowKey = GetRowKey(aggregate.BatchId),
                State = aggregate.State,
                StartedAt = aggregate.StartMoment,
                SuspendedAt = aggregate.SuspendMoment,
                FinishedAt = aggregate.FinishMoment,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                BlockchainType = aggregate.BlockchainType,
                IncludeFee = aggregate.IncludeFee,
                BatchId = aggregate.BatchId,
                Cashouts = aggregate.Cashouts
                    .Select(BatchedCashoutEntity.FromDomain)
                    .ToArray(),
                HotWalletAddress = aggregate.HotWalletAddress,
                TransactionHash = aggregate.TransactionHash,
                TransactionOutputs = aggregate.TransactionOutputs
                    .Select(OperationOutputEntity.FromDomain)
                    .ToArray(),
                TransactionFee = aggregate.TransactionFee,
                TransactionBlock = aggregate.TransactionBlock
            };
        }

        public CashoutsBatchAggregate ToDomain()
        {
            return CashoutsBatchAggregate.Restore(
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
                HotWalletAddress,
                TransactionHash,
                TransactionOutputs
                    .Select(x => x.ToDomain())
                    .ToArray(),
                TransactionFee,
                TransactionBlock);
        }

        #endregion
    }
}
