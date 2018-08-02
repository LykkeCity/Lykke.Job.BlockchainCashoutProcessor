using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.ActiveBatch
{
    public interface IActiveBatchRepository
    {
        Task<bool> DeleteIfExistAsync(string blockchainType,
            string hotWalletAddress, 
            string blockchainAssetId,
            Guid batchId);

        Task<ActiveBatchAggregate> GetOrAddAsync(string blockchainType,
            string hotWalletAddress, 
            string blockchainAssetId,
            Func<ActiveBatchAggregate> newAggregateFactory);

        Task<ActiveBatchAggregate> TryGetAsync(string blockchainType,
            string hotWalletAddress,
            string blockchainAssetId,
            Guid batchId);

        Task<IEnumerable<ActiveBatchAggregate>> GetAsync(string blockchainType);

        Task SaveAsync(ActiveBatchAggregate aggregate);
    }
}
