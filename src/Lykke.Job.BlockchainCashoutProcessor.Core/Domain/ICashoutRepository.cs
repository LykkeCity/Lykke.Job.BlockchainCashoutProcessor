using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain
{
    public interface ICashoutRepository
    {
        Task<CashoutAggregate> GetOrAddAsync(Guid operationId, Func<CashoutAggregate> newAggregateFactory);
        Task<CashoutAggregate> GetAsync(Guid operationId);
        Task SaveAsync(CashoutAggregate aggregate);
        Task<CashoutAggregate> TryGetAsync(Guid operationId);
    }
}
