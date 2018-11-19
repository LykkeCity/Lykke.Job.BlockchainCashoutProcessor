using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
            var persistEntityBlock = new ActionBlock<ClosedBatchedCashoutEntity>
            (
                entity => _storage.InsertOrReplaceAsync(entity),
                new ExecutionDataflowBlockOptions
                {
                    EnsureOrdered = false,
                    MaxDegreeOfParallelism = 8
                }
            );

            foreach (var entity in processedCashoutIds.Select(ClosedBatchedCashoutEntity.FromDomain))
            {
                if (!persistEntityBlock.Post(entity))
                {
                    throw new InvalidOperationException("Can't post entity to the action block");
                }
            }

            persistEntityBlock.Complete();

            return persistEntityBlock.Completion;
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
