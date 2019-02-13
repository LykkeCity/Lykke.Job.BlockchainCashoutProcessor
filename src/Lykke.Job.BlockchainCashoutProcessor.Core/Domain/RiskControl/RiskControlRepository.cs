using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.RiskControl
{
    public interface IRiskControlRepository
    {
        Task<RiskControlAggregate> GetOrAddAsync(Guid operationId, Func<RiskControlAggregate> newAggregateFactory);
        Task SaveAsync(RiskControlAggregate aggregate);
        Task<RiskControlAggregate> TryGetAsync(Guid operationId);
    }
}
