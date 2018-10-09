using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching
{
    public class ActiveCashoutsBatchIdRepository : IActiveCashoutsBatchIdRepository
    {
        private readonly INoSQLTableStorage<ActiveCashoutsBatchIdEntity> _storage;

        public static IActiveCashoutsBatchIdRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<ActiveCashoutsBatchIdEntity>.Create(
                connectionString,
                "ActiveCashoutBatch",
                log);

            return new ActiveCashoutsBatchIdRepository(storage);
        }

        private ActiveCashoutsBatchIdRepository(INoSQLTableStorage<ActiveCashoutsBatchIdEntity> storage)
        {
            _storage = storage;
        }

        public async Task<ActiveCashoutBatchId> GetActiveOrNextBatchId(string blockchainType, string blockchainAssetId, string hotWallet, Func<Guid> getNextId)
        {
            var partitionKey = ActiveCashoutsBatchIdEntity.GeneratePartitionKey(blockchainType);
            var rowKey = ActiveCashoutsBatchIdEntity.GenerateRowKey(blockchainAssetId, hotWallet);
            
            var entity = await _storage.GetOrInsertAsync(
                partitionKey,
                rowKey,
                () =>
                {
                    var id = getNextId();

                    return ActiveCashoutsBatchIdEntity.FromDomain
                    (
                        blockchainType,
                        blockchainAssetId,
                        hotWallet,
                        id
                    );
                });

            return entity.ToDomain();
        }

        public async Task RevokeActiveIdAsync(string blockchainType, string blockchainAssetId, string hotWallet, Guid batchId)
        {
            var partitionKey = ActiveCashoutsBatchIdEntity.GeneratePartitionKey(blockchainType);
            var rowKey = ActiveCashoutsBatchIdEntity.GenerateRowKey(blockchainAssetId, hotWallet);

            await _storage.DeleteIfExistAsync(partitionKey, rowKey, e => e.BatchId == batchId);
        }
    }
}
