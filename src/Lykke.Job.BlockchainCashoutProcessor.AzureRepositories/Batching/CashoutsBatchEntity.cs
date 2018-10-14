using System;
using System.Linq;
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

        public DateTime StartMoment { get; set; }
        public DateTime? LastCashoutAdditionMoment { get; set; }
        public DateTime? ExpirationMoment { get; set; }
        public DateTime? ClosingMoment { get; set; }
        public DateTime? IdRevocationMoment { get; set; }
        public DateTime? FinishMoment { get; set; }
        public Guid BatchId { get; set; }
        public string BlockchainType { get; set; }
        public string AssetId { get; set; }
        public string BlockchainAssetId { get; set; }
        public string HotWalletAddress { get; set; }
        public int CountThreshold { get; set; }
        public TimeSpan AgeThreshold { get; set; }
        [JsonValueSerializer]
        public BatchedCashoutEntity[] Cashouts { get; set; }
        public CashoutsBatchState State { get; set; }
        public CashoutsBatchClosingReason ClosingReason { get; set; }

        #endregion
        

        #region Keys

        public static string GetPartitionKey(Guid operationId)
        {
            return $"{operationId:D}";
        }

        public static string GetRowKey(Guid operationId)
        {
            return "aggregate";
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
                StartMoment = aggregate.StartMoment,
                LastCashoutAdditionMoment = aggregate.LastCashoutAdditionMoment,
                ExpirationMoment = aggregate.ExpirationMoment,
                ClosingMoment = aggregate.ClosingMoment,
                IdRevocationMoment = aggregate.IdRevocationMoment,
                FinishMoment = aggregate.FinishMoment,
                BatchId = aggregate.BatchId,
                BlockchainType = aggregate.BlockchainType,
                AssetId = aggregate.AssetId,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                HotWalletAddress = aggregate.HotWalletAddress,
                CountThreshold = aggregate.CountThreshold,
                AgeThreshold = aggregate.AgeThreshold,
                Cashouts = aggregate.Cashouts
                    .Select(BatchedCashoutEntity.FromDomain)
                    .ToArray(),
                State = aggregate.State,
                ClosingReason = aggregate.ClosingReason
            };
        }

        public CashoutsBatchAggregate ToDomain()
        {
            return CashoutsBatchAggregate.Restore(
                ETag,
                StartMoment,
                LastCashoutAdditionMoment,
                ExpirationMoment,
                ClosingMoment,
                IdRevocationMoment,
                FinishMoment,
                BatchId,
                BlockchainType,
                AssetId,
                BlockchainAssetId,
                HotWalletAddress,
                CountThreshold,
                AgeThreshold,
                Cashouts
                    .Select(x => x.ToDomain())
                    .ToHashSet(),
                State,
                ClosingReason);
        }

        #endregion
    }
}
