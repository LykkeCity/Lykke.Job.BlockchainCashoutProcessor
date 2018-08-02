using System;
using System.Collections.Generic;
using System.Text;
using Common;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batch;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batch
{
    internal class CashoutBatchEntity:TableEntity
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

        public string ToOperations { get; set; }

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
                ToOperations = aggregate.ToOperations.ToJson(),
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
                ToOperations.DeserializeJson<(Guid operationId, decimal amount, string destinationAddress)[]>(),
                HotWalletAddress);
        }

        #endregion
    }
}
