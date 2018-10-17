using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public interface ICashoutsBatchReadOnlyRepository
    {
        Task<CashoutsBatchAggregate> GetAsync(Guid batchId);
        Task<CashoutsBatchAggregate> TryGetAsync(Guid batchId);
    }
}
