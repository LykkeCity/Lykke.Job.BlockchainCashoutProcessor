using System;
using System.Threading.Tasks;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batch;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Repositories
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

        Task SaveAsync(ActiveBatchAggregate aggregate);
    }
}
