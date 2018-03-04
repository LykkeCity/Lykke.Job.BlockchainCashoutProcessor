using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories
{
    [UsedImplicitly]
    public class CrossClientCashoutRepository : ICrossClientCashoutRepository
    {
        private readonly INoSQLTableStorage<CrossClientCashoutEntity> _storage;

        public static ICrossClientCashoutRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<CrossClientCashoutEntity>.Create(
                connectionString,
                "CrossClientCashout",
                log);

            return new CrossClientCashoutRepository(storage);
        }

        private CrossClientCashoutRepository(INoSQLTableStorage<CrossClientCashoutEntity> storage)
        {
            _storage = storage;
        }

        public async Task<CrossClientCashoutAggregate> GetOrAddAsync(Guid operationId, Func<CrossClientCashoutAggregate> newAggregateFactory)
        {
            var partitionKey = CrossClientCashoutEntity.GetPartitionKey(operationId);
            var rowKey = CrossClientCashoutEntity.GetRowKey(operationId);

            var startedEntity = await _storage.GetOrInsertAsync(
                partitionKey,
                rowKey,
                () =>
                {
                    var newAggregate = newAggregateFactory();

                    return CrossClientCashoutEntity.FromDomain(newAggregate);
                });

            return startedEntity.ToDomain();
        }

        public async Task<CrossClientCashoutAggregate> GetAsync(Guid operationId)
        {
            var aggregate = await TryGetAsync(operationId);

            if (aggregate == null)
            {
                throw new InvalidOperationException($"Cashout with operation ID [{operationId}] is not found");
            }

            return aggregate;
        }

        public async Task<CrossClientCashoutAggregate> TryGetAsync(Guid operationId)
        {
            var partitionKey = CrossClientCashoutEntity.GetPartitionKey(operationId);
            var rowKey = CrossClientCashoutEntity.GetRowKey(operationId);

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            return entity?.ToDomain();
        }

        public async Task SaveAsync(CrossClientCashoutAggregate aggregate)
        {
            var entity = CrossClientCashoutEntity.FromDomain(aggregate);

            await _storage.ReplaceAsync(entity);
        }

    }
}
