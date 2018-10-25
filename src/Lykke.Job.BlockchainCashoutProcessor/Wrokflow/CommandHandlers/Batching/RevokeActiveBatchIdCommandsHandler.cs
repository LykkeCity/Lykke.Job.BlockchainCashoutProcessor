using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.StateMachine;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batching
{
    [UsedImplicitly]
    public class RevokeActiveBatchIdCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly IActiveCashoutsBatchIdRepository _activeCashoutsBatchIdRepository;
        private readonly ICashoutsBatchRepository _cashoutsBatchRepository;

        public RevokeActiveBatchIdCommandsHandler(
            IChaosKitty chaosKitty,
            IActiveCashoutsBatchIdRepository activeCashoutsBatchIdRepository,
            ICashoutsBatchRepository cashoutsBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _activeCashoutsBatchIdRepository = activeCashoutsBatchIdRepository;
            _cashoutsBatchRepository = cashoutsBatchRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(RevokeActiveBatchIdCommand command, IEventPublisher publisher)
        {
            var batch = await _cashoutsBatchRepository.GetAsync(command.BatchId);
            var transitionResult = await batch.RevokeIdAsync(_activeCashoutsBatchIdRepository);

            _chaosKitty.Meow(command.BatchId);

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashoutsBatchRepository.SaveAsync(batch);

                _chaosKitty.Meow(command.BatchId);
            }

            if (transitionResult.ShouldPublishEvents())
            {
                publisher.PublishEvent
                (
                    new ActiveBatchIdRevokedEvent
                    {
                        BatchId = batch.BatchId
                    }
                );

                _chaosKitty.Meow(command.BatchId);
            }

            return CommandHandlingResult.Ok();
        }
    }
}
