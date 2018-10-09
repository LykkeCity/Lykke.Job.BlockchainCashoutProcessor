using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public interface ICashoutsBatchRepository : ICashoutsBatchReadOnlyRepository
    {
        Task<CashoutsBatchAggregate> GetOrAddAsync(Guid batchId, Func<CashoutsBatchAggregate> newAggregateFactory);
        Task SaveAsync(CashoutsBatchAggregate aggregate);
    }
}
