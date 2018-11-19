﻿using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.Core.Repositories;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.CrossClient
{
    public class MatchingEngineCallsDeduplicationRepository : IMatchingEngineCallsDeduplicationRepository
    {
        private readonly INoSQLTableStorage<MatchingEngineCallsDeduplicationEntity> _storage;

        public static IMatchingEngineCallsDeduplicationRepository Create(IReloadingManager<string> connectionString, ILogFactory logFactory)
        {
            var storage = AzureTableStorage<MatchingEngineCallsDeduplicationEntity>.Create(
                connectionString,
                "CrossClientCashoutMatchingEngineCallsDeduplication",
                logFactory);

            return new MatchingEngineCallsDeduplicationRepository(storage);
        }

        private MatchingEngineCallsDeduplicationRepository(INoSQLTableStorage<MatchingEngineCallsDeduplicationEntity> storage)
        {
            _storage = storage;
        }

        public Task InsertOrReplaceAsync(Guid operationId)
        {
            return _storage.InsertOrReplaceAsync(new MatchingEngineCallsDeduplicationEntity
            {
                PartitionKey = MatchingEngineCallsDeduplicationEntity.GetPartitionKey(operationId),
                RowKey = MatchingEngineCallsDeduplicationEntity.GetRowKey(operationId)
            });
        }

        public async Task<bool> IsExistsAsync(Guid operationId)
        {
            var partitionKey = MatchingEngineCallsDeduplicationEntity.GetPartitionKey(operationId);
            var rowKey = MatchingEngineCallsDeduplicationEntity.GetRowKey(operationId);

            return await _storage.GetDataAsync(partitionKey, rowKey) != null;
        }

        public Task TryRemoveAsync(Guid operationId)
        {
            var partitionKey = MatchingEngineCallsDeduplicationEntity.GetPartitionKey(operationId);
            var rowKey = MatchingEngineCallsDeduplicationEntity.GetRowKey(operationId);

            return _storage.DeleteIfExistAsync(partitionKey, rowKey);
        }
    }
}
