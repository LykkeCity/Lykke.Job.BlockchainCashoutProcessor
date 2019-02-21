using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.RiskControl;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.RiskControl
{
    [UsedImplicitly]
    public class CashoutRiskControlRepository : ICashoutRiskControlRepository
    {
        private readonly INoSQLTableStorage<CashoutRiskControlEntity> _storage;

        public static ICashoutRiskControlRepository Create(IReloadingManager<string> connectionString, ILogFactory logFactory)
        {
            var storage = AzureTableStorage<CashoutRiskControlEntity>.Create(
                connectionString,
                "CashoutRiskControl",
                logFactory);

            return new CashoutRiskControlRepository(storage);
        }

        private CashoutRiskControlRepository(INoSQLTableStorage<CashoutRiskControlEntity> storage)
        {
            _storage = storage;
        }

        public async Task<CashoutRiskControlAggregate> GetOrAddAsync(Guid operationId, Func<CashoutRiskControlAggregate> newAggregateFactory)
        {
            var partitionKey = CashoutRiskControlEntity.GetPartitionKey(operationId);
            var rowKey = CashoutRiskControlEntity.GetRowKey();

            var startedEntity = await _storage.GetOrInsertAsync(
                partitionKey,
                rowKey,
                () =>
                {
                    var newAggregate = newAggregateFactory();

                    return CashoutRiskControlEntity.FromDomain(newAggregate);
                });

            return startedEntity.ToDomain();
        }

        public async Task<CashoutRiskControlAggregate> TryGetAsync(Guid operationId)
        {
            var partitionKey = CashoutRiskControlEntity.GetPartitionKey(operationId);
            var rowKey = CashoutRiskControlEntity.GetRowKey();

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            return entity?.ToDomain();
        }

        public async Task SaveAsync(CashoutRiskControlAggregate aggregate)
        {
            var entity = CashoutRiskControlEntity.FromDomain(aggregate);

            await _storage.ReplaceAsync(entity);
        }

    }
}
