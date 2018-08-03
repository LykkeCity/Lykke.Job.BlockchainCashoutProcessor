using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.ActiveBatch;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories
{
    public class ActiveBatchRepository:IActiveBatchRepository
    {
        private readonly INoSQLTableStorage<ActiveBatchEntity> _storage;

        public static IActiveBatchRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<ActiveBatchEntity>.Create(
                connectionString,
                "ActiveBatch",
                log);

            return new ActiveBatchRepository(storage);
        }

        private ActiveBatchRepository(INoSQLTableStorage<ActiveBatchEntity> storage)
        {
            _storage = storage;
        }

        public  Task<bool> DeleteIfExistAsync(string blockchainType,
            string hotWalletAddress, 
            string blockchainAssetId,
            Guid batchId)
        {
            var partitionKey = ActiveBatchEntity.GeneratePartitionKey(blockchainType);
            var rowKey = ActiveBatchEntity.GenerateRowKey(blockchainAssetId, hotWalletAddress);

            return _storage.DeleteIfExistAsync(partitionKey, rowKey, e => e.BatchId == batchId);
        }

        public async Task<ActiveBatchAggregate> GetOrAddAsync(string blockchainType,
            string hotWalletAddress, 
            string blockchainAssetId, 
            Func<ActiveBatchAggregate> newAggregateFactory)
        {
            var partitionKey = ActiveBatchEntity.GeneratePartitionKey(blockchainType);
            var rowKey = ActiveBatchEntity.GenerateRowKey(blockchainAssetId, hotWalletAddress);


            var entity = await _storage.GetOrInsertAsync(
                partitionKey,
                rowKey,
                () =>
                {
                    var newAggregate = newAggregateFactory();

                    return ActiveBatchEntity.FromDomain(newAggregate);
                });

            return entity.ToDomain();
        }

        public async Task<ActiveBatchAggregate> TryGetAsync(string blockchainType,
            string hotWalletAddress,
            string blockchainAssetId,
            Guid batchId)
        {
            var partitionKey = ActiveBatchEntity.GeneratePartitionKey(blockchainType);
            var rowKey = ActiveBatchEntity.GenerateRowKey(blockchainAssetId, hotWalletAddress);

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            return entity?.BatchId == batchId ? entity.ToDomain() : null;
        }

        public async Task<IEnumerable<ActiveBatchAggregate>> GetAsync(string blockchainType)
        {
            var partitionKey = ActiveBatchEntity.GeneratePartitionKey(blockchainType);

            return (await _storage.GetDataAsync(partitionKey)).Select(p => p.ToDomain());
        }

        public async Task SaveAsync(ActiveBatchAggregate aggregate)
        {
            var entity = ActiveBatchEntity.FromDomain(aggregate);

            await _storage.ReplaceAsync(entity);
        }
    }
}
