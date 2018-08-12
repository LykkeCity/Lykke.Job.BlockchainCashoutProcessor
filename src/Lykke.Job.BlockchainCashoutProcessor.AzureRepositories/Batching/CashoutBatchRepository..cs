using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching
{
    public class CashoutBatchRepository:ICashoutBatchRepository
    {
        private readonly INoSQLTableStorage<CashoutBatchEntity> _storage;

        public static ICashoutBatchRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<CashoutBatchEntity>.Create(
                connectionString,
                "CashoutBatch",
                log);

            return new CashoutBatchRepository(storage);
        }

        private CashoutBatchRepository(INoSQLTableStorage<CashoutBatchEntity> storage)
        {
            _storage = storage;
        }


        public async Task<CashoutBatchAggregate> GetOrAddAsync(Guid batchId, Func<CashoutBatchAggregate> newAggregateFactory)
        {
            var partitionKey = CashoutBatchEntity.GetPartitionKey(batchId);
            var rowKey = CashoutBatchEntity.GetRowKey(batchId);

            var startedEntity = await _storage.GetOrInsertAsync(
                partitionKey,
                rowKey,
                () =>
                {
                    var newAggregate = newAggregateFactory();

                    return CashoutBatchEntity.FromDomain(newAggregate);
                });

            return startedEntity.ToDomain();
        }

        public async Task SaveAsync(CashoutBatchAggregate aggregate)
        {
            var entity = CashoutBatchEntity.FromDomain(aggregate);

            await _storage.ReplaceAsync(entity);
        }

        public async Task<CashoutBatchAggregate> GetAsync(Guid batchId)
        {
            var aggregate = await TryGetAsync(batchId);

            if (aggregate == null)
            {
                throw new InvalidOperationException($"Cashout batch with  ID [{batchId}] is not found");
            }

            return aggregate;
        }

        public async Task<CashoutBatchAggregate> TryGetAsync(Guid batchId)
        {
            var partitionKey = CashoutBatchEntity.GetPartitionKey(batchId);
            var rowKey = CashoutBatchEntity.GetRowKey(batchId);

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            return entity?.ToDomain();
        }
    }
}
