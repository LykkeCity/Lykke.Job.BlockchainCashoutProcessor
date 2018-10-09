using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching
{
    public class CashoutsBatchRepository:ICashoutsBatchRepository
    {
        private readonly INoSQLTableStorage<CashoutsBatchEntity> _storage;

        public static ICashoutsBatchRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<CashoutsBatchEntity>.Create(
                connectionString,
                "CashoutBatch",
                log);

            return new CashoutsBatchRepository(storage);
        }

        private CashoutsBatchRepository(INoSQLTableStorage<CashoutsBatchEntity> storage)
        {
            _storage = storage;
        }


        public async Task<CashoutsBatchAggregate> GetOrAddAsync(Guid batchId, Func<CashoutsBatchAggregate> newAggregateFactory)
        {
            var partitionKey = CashoutsBatchEntity.GetPartitionKey(batchId);
            var rowKey = CashoutsBatchEntity.GetRowKey(batchId);

            var startedEntity = await _storage.GetOrInsertAsync(
                partitionKey,
                rowKey,
                () =>
                {
                    var newAggregate = newAggregateFactory();

                    return CashoutsBatchEntity.FromDomain(newAggregate);
                });

            return startedEntity.ToDomain();
        }

        public async Task SaveAsync(CashoutsBatchAggregate aggregate)
        {
            var entity = CashoutsBatchEntity.FromDomain(aggregate);

            await _storage.ReplaceAsync(entity);
        }

        public async Task<CashoutsBatchAggregate> GetAsync(Guid batchId)
        {
            var aggregate = await TryGetAsync(batchId);

            if (aggregate == null)
            {
                throw new InvalidOperationException($"Cashout batch with  ID [{batchId}] is not found");
            }

            return aggregate;
        }

        public async Task<CashoutsBatchAggregate> TryGetAsync(Guid batchId)
        {
            var partitionKey = CashoutsBatchEntity.GetPartitionKey(batchId);
            var rowKey = CashoutsBatchEntity.GetRowKey(batchId);

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            return entity?.ToDomain();
        }
    }
}
