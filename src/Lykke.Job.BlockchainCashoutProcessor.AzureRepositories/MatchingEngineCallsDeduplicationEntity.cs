using Common;
using Lykke.AzureStorage.Tables;
using System;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories
{
    internal class MatchingEngineCallsDeduplicationEntity : AzureTableEntity
    {
        public static string GetPartitionKey(Guid operationId)
        {
            // Adds hash to distribute all records to the different partitions
            var hash = operationId.ToString().CalculateHexHash32(3);

            return hash;
        }

        public static string GetRowKey(Guid operationId)
        {
            return operationId.ToString("D");
        }
    }
}
