using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching
{
    public class ClosedBatchedCashoutEntity : AzureTableEntity
    {
        [UsedImplicitly]
        public Guid CashoutId { get; set; }

        public static string GetPartitionKey(Guid cashoutId)
        {
            return $"{cashoutId:D}";
        }

        public static string GetRowKey()
        {
            return "batched-cashout";
        }

        public static ClosedBatchedCashoutEntity FromDomain(Guid cashoutId)
        {
            return new ClosedBatchedCashoutEntity
            {
                PartitionKey = GetPartitionKey(cashoutId),
                RowKey = GetRowKey(),
                CashoutId = cashoutId
            };
        }
    }
}
