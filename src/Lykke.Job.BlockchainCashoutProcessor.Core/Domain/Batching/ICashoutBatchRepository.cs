using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public interface ICashoutBatchRepository
    {
        Task<CashoutBatchAggregate> GetOrAddAsync(Guid batchId, Func<CashoutBatchAggregate> newAggregateFactory);
        Task SaveAsync(CashoutBatchAggregate aggregate);
        Task<CashoutBatchAggregate> GetAsync(Guid batchId);
        Task<CashoutBatchAggregate> TryGetAsync(Guid batchId);
    }
}
