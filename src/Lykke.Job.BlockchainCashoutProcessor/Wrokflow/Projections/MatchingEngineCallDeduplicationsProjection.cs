using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Core.Repositories;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.CrossClient;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Projections
{
    public class MatchingEngineCallDeduplicationsProjection
    {
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;
        private readonly ICrossClientCashoutRepository _crossClientCashoutRepository;
        private readonly IChaosKitty _chaosKitty;

        public MatchingEngineCallDeduplicationsProjection(
            IMatchingEngineCallsDeduplicationRepository deduplicationRepository,
            ICrossClientCashoutRepository crossClientCashoutRepository,
            IChaosKitty chaosKitty)
        {
            _deduplicationRepository = deduplicationRepository;
            _crossClientCashoutRepository = crossClientCashoutRepository;
            _chaosKitty = chaosKitty;
        }

        [UsedImplicitly]
        public async Task Handle(CashinEnrolledToMatchingEngineEvent evt)
        {
            var aggregate = await _crossClientCashoutRepository.GetAsync(evt.CashoutOperationId);

            await _deduplicationRepository.TryRemoveAsync(aggregate.CashinOperationId);

            _chaosKitty.Meow(evt.CashoutOperationId);
        }
    }
}
