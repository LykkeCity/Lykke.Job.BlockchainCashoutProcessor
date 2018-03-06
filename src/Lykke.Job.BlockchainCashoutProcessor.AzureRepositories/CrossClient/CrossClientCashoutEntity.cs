using System;
using Common;
using Lykke.AzureStorage.Tables;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.CrossClient;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.CrossClient
{
    internal class CrossClientCashoutEntity : AzureTableEntity
    {
        #region Fields

        // ReSharper disable MemberCanBePrivate.Global

        public CrossClientCashoutState State { get; set; }
        public DateTime StartMoment { get; set; }
        public Guid OperationId { get; set; }
        public Guid ClientId { get; set; }
        public string BlockchainType { get; set; }
        public string BlockchainAssetId { get; set; }
        public string HotWalletAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public string AssetId { get; set; }
        public DateTime? MatchingEngineEnrollementMoment { get; private set; }
        public Guid RecipientClientId { get; private set; }
        public Guid CashinOperationId { get; private set; }

        // ReSharper restore MemberCanBePrivate.Global

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

        public static CrossClientCashoutEntity FromDomain(CrossClientCashoutAggregate aggregate)
        {
            return new CrossClientCashoutEntity
            {
                ETag = string.IsNullOrEmpty(aggregate.Version) ? "*" : aggregate.Version,
                PartitionKey = GetPartitionKey(aggregate.OperationId),
                RowKey = GetRowKey(aggregate.OperationId),
                State = aggregate.State,
                StartMoment = aggregate.StartMoment,
                OperationId = aggregate.OperationId,
                ClientId = aggregate.ClientId,
                BlockchainType = aggregate.BlockchainType,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                HotWalletAddress = aggregate.HotWalletAddress,
                ToAddress = aggregate.ToAddress,
                Amount = aggregate.Amount,
                AssetId = aggregate.AssetId,
                MatchingEngineEnrollementMoment = aggregate.MatchingEngineEnrollementMoment,
                RecipientClientId = aggregate.RecipientClientId,
                CashinOperationId = aggregate.CashinOperationId
            };
        }

        public CrossClientCashoutAggregate ToDomain()
        {
            return CrossClientCashoutAggregate.Restore(
                ETag,
                State,
                StartMoment,
                OperationId,
                ClientId,
                BlockchainType,
                BlockchainAssetId,
                HotWalletAddress,
                ToAddress,
                Amount,
                AssetId,
                MatchingEngineEnrollementMoment,
                RecipientClientId,
                CashinOperationId);
        }

        #endregion
    }
}
