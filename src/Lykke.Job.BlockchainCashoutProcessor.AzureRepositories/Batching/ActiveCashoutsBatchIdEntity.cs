using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    internal class ActiveCashoutsBatchIdEntity : AzureTableEntity
    {
        #region Fields

        public Guid BatchId { get; set; }

        #endregion


        #region Keys
        
        public static string GeneratePartitionKey(string blockchainType)
        {
            return blockchainType;
        }

        public static string GenerateRowKey(string blockchainAssetId, string hotWallet)
        {
            return $"{blockchainAssetId}-{hotWallet}";
        }

        #endregion


        #region Conversion

        public static ActiveCashoutsBatchIdEntity FromDomain(
            string blockchainType,
            string blockchainAssetId,
            string hotWallet,
            Guid batchId)
        {
            return new ActiveCashoutsBatchIdEntity
            {
                PartitionKey = GeneratePartitionKey(blockchainType),
                RowKey = GenerateRowKey(blockchainAssetId, hotWallet),
                BatchId = batchId
            };
        }

        public ActiveCashoutBatchId ToDomain()
        {
            return ActiveCashoutBatchId.Create(BatchId);
        }
        
        #endregion
    }
}
