using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching
{
    public class ClosedBatchedCashoutRepository : IClosedBatchedCashoutRepository
    {
        private readonly INoSQLTableStorage<ClosedBatchedCashoutEntity> _storage;

        public static IClosedBatchedCashoutRepository Create(IReloadingManager<string> connectionString, ILogFactory logFactory)
        {
            var storage = AzureTableStorage<ClosedBatchedCashoutEntity>.Create(
                connectionString,
                "ClosedBatchedCashout",
                logFactory);

            return new ClosedBatchedCashoutRepository(storage);
        }

        private ClosedBatchedCashoutRepository(INoSQLTableStorage<ClosedBatchedCashoutEntity> storage)
        {
            _storage = storage;
        }

        public Task EnsureClosedAsync(IEnumerable<Guid> processedCashoutIds)
        {
            return _storage.InsertOrReplaceBatchAsync(processedCashoutIds.Select(ClosedBatchedCashoutEntity.FromDomain));
        }

        public async Task<bool> IsCashoutClosedAsync(Guid cashoutId)
        {
            var partitionKey = ClosedBatchedCashoutEntity.GetPartitionKey(cashoutId);
            var rowKey = ClosedBatchedCashoutEntity.GetRowKey();

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            return entity != null;
        }
    }
}
