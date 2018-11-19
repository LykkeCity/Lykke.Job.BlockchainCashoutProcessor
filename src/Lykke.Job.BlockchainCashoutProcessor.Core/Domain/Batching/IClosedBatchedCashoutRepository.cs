using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public interface IClosedBatchedCashoutRepository
    {
        Task EnsureClosedAsync(IEnumerable<Guid> processedCashoutIds);
        Task<bool> IsCashoutClosedAsync(Guid cashoutId);
    }
}
