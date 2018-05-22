using System;
using Common;
using Lykke.AzureStorage.Tables;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories
{
    internal class CashoutEntity : AzureTableEntity
    {
        #region Fields

        // ReSharper disable MemberCanBePrivate.Global

        public CashoutState State { get; set; }
        public CashoutResult Result { get; set; }

        public DateTime StartMoment { get; set; }
        public DateTime? OperationFinishMoment { get; set; }

        public Guid OperationId { get; set; }
        public Guid ClientId { get; set; }
        public string BlockchainType { get; set; }
        public string BlockchainAssetId { get; set; }
        public string HotWalletAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public string AssetId { get; set; }

        public string TransactionHash { get; set; }
        public decimal? TransactionAmount { get; set; }
        public decimal? Fee { get; set; }
        public string Error { get; set; }

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

        public static CashoutEntity FromDomain(CashoutAggregate aggregate)
        {
            return new CashoutEntity
            {
                ETag = string.IsNullOrEmpty(aggregate.Version) ? "*" : aggregate.Version,
                PartitionKey = GetPartitionKey(aggregate.OperationId),
                RowKey = GetRowKey(aggregate.OperationId),
                State = aggregate.State,
                Result = aggregate.Result,
                StartMoment = aggregate.StartMoment,
                OperationFinishMoment = aggregate.OperationFinishMoment,
                OperationId = aggregate.OperationId,
                ClientId = aggregate.ClientId,
                BlockchainType = aggregate.BlockchainType,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                HotWalletAddress = aggregate.HotWalletAddress,
                ToAddress = aggregate.ToAddress,
                Amount = aggregate.Amount,
                AssetId = aggregate.AssetId,
                TransactionHash = aggregate.TransactionHash,
                TransactionAmount = aggregate.TransactionAmount,
                Fee = aggregate.Fee,
                Error = aggregate.Error
            };
        }

        public CashoutAggregate ToDomain()
        {
            return CashoutAggregate.Restore(
                ETag,
                State,
                Result,
                StartMoment,
                OperationFinishMoment,
                OperationId,
                ClientId,
                BlockchainType,
                BlockchainAssetId,
                HotWalletAddress,
                ToAddress,
                Amount,
                AssetId,
                TransactionHash,
                TransactionAmount,
                Fee,
                Error);
        }

        #endregion
    }
}
