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
    public class CashoutRepository : ICashoutRepository
    {
        private readonly INoSQLTableStorage<CashoutEntity> _storage;

        public static ICashoutRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<CashoutEntity>.Create(
                connectionString,
                "Cashout",
                log);

            return new CashoutRepository(storage);
        }

        private CashoutRepository(INoSQLTableStorage<CashoutEntity> storage)
        {
            _storage = storage;
        }

        public async Task<CashoutAggregate> GetOrAddAsync(Guid operationId, Func<CashoutAggregate> newAggregateFactory)
        {
            var partitionKey = CashoutEntity.GetPartitionKey(operationId);
            var rowKey = CashoutEntity.GetRowKey(operationId);

            var startedEntity = await _storage.GetOrInsertAsync(
                partitionKey,
                rowKey,
                () =>
                {
                    var newAggregate = newAggregateFactory();

                    return CashoutEntity.FromDomain(newAggregate);
                });

            return startedEntity.ToDomain();
        }

        public async Task<CashoutAggregate> GetAsync(Guid operationId)
        {
            var aggregate = await TryGetAsync(operationId);

            if (aggregate == null)
            {
                throw new InvalidOperationException($"Cashout with operation ID [{operationId}] is not found");
            }

            return aggregate;
        }

        public async Task<CashoutAggregate> TryGetAsync(Guid operationId)
        {
            var partitionKey = CashoutEntity.GetPartitionKey(operationId);
            var rowKey = CashoutEntity.GetRowKey(operationId);

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            return entity?.ToDomain();
        }

        public async Task SaveAsync(CashoutAggregate aggregate)
        {
            var entity = CashoutEntity.FromDomain(aggregate);

            await _storage.ReplaceAsync(entity);
        }

    }
}
