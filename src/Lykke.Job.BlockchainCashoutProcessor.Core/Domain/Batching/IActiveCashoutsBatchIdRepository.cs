using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public interface IActiveCashoutsBatchIdRepository
    {
        Task<ActiveCashoutBatchId> GetActiveOrNextBatchId(string blockchainType, string blockchainAssetId, string hotWallet, Func<Guid> getNextId);
        Task RevokeActiveIdAsync(string blockchainType, string blockchainAssetId, string hotWallet, Guid batchBatchId);
    }
}
