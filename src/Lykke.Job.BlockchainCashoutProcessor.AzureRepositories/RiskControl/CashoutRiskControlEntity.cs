using System;
using Common;
using Lykke.AzureStorage.Tables;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.RiskControl;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.RiskControl
{
    internal class CshoutRiskControlEntity : AzureTableEntity
    {
        #region Fields

        // ReSharper disable MemberCanBePrivate.Global

        public CashoutRiskControlState State { get; set; }
        public CashoutRiskControlResult Result { get; set; }

        public DateTime CreationMoment { get; set; }
        public DateTime? StartMoment { get; set; }
        public DateTime? OperationAcceptanceMoment { get; set; }
        public DateTime? OperationRejectionMoment { get; set; }

        public Guid OperationId { get; set; }
        public Guid ClientId { get; set; }
        public string BlockchainType { get; set; }
        public string BlockchainAssetId { get; set; }
        public string HotWalletAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public string AssetId { get; set; }
        public string Error { get; set; }

        // ReSharper restore MemberCanBePrivate.Global

        #endregion

        #region Keys

        public static string GetPartitionKey(Guid operationId) => $"{operationId:D}";
        public static string GetRowKey() => string.Empty;

        #endregion

        #region Conversion

        public static CshoutRiskControlEntity FromDomain(CashoutRiskControlAggregate aggregate)
        {
            return new CshoutRiskControlEntity
            {
                ETag = string.IsNullOrEmpty(aggregate.Version) ? "*" : aggregate.Version,
                PartitionKey = GetPartitionKey(aggregate.OperationId),
                RowKey = GetRowKey(),
                State = aggregate.State,
                Result = aggregate.Result,
                CreationMoment = aggregate.CreationMoment,
                StartMoment = aggregate.StartMoment,
                OperationAcceptanceMoment = aggregate.OperationAcceptanceMoment,
                OperationRejectionMoment = aggregate.OperationRejectionMoment,
                OperationId = aggregate.OperationId,
                ClientId = aggregate.ClientId,
                AssetId = aggregate.AssetId,
                BlockchainType = aggregate.BlockchainType,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                HotWalletAddress = aggregate.HotWalletAddress,
                ToAddress = aggregate.ToAddress,
                Amount = aggregate.Amount,
                Error = aggregate.Error
            };
        }

        public CashoutRiskControlAggregate ToDomain()
        {
            return CashoutRiskControlAggregate.Restore(
                ETag,
                State,
                Result,
                CreationMoment,
                StartMoment,
                OperationAcceptanceMoment,
                OperationRejectionMoment,
                OperationId,
                ClientId,
                AssetId,
                BlockchainType,
                BlockchainAssetId,
                HotWalletAddress,
                ToAddress,
                Amount,
                Error);
        }

        #endregion
    }
}
