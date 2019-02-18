using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.RiskControl
{
    public interface ICashoutRiskControlRepository
    {
        Task<CashoutRiskControlAggregate> GetOrAddAsync(Guid operationId, Func<CashoutRiskControlAggregate> newAggregateFactory);
        Task SaveAsync(CashoutRiskControlAggregate aggregate);
        Task<CashoutRiskControlAggregate> TryGetAsync(Guid operationId);
    }
}
