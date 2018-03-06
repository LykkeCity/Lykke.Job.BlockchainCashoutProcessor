using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.CrossClient
{
    public interface ICrossClientCashoutRepository
    {
        Task<CrossClientCashoutAggregate> GetOrAddAsync(Guid operationId, Func<CrossClientCashoutAggregate> newAggregateFactory);
        Task<CrossClientCashoutAggregate> GetAsync(Guid operationId);
        Task SaveAsync(CrossClientCashoutAggregate aggregate);
        Task<CrossClientCashoutAggregate> TryGetAsync(Guid operationId);
    }
}
