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
    public class CloseBatchCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ICashoutsBatchRepository _cashoutsBatchRepository;
        private readonly IClosedBatchedCashoutRepository _closedBatchedCashoutRepository;

        public CloseBatchCommandsHandler(
            IChaosKitty chaosKitty,
            ICashoutsBatchRepository cashoutsBatchRepository,
            IClosedBatchedCashoutRepository closedBatchedCashoutRepository)
        {
            _chaosKitty = chaosKitty;
            _cashoutsBatchRepository = cashoutsBatchRepository;
            _closedBatchedCashoutRepository = closedBatchedCashoutRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(CloseBatchCommand command, IEventPublisher publisher)
        {
            var batch = await _cashoutsBatchRepository.GetAsync(command.BatchId);
            var transitionResult = await batch.CloseAsync(command.Reason, _closedBatchedCashoutRepository);

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashoutsBatchRepository.SaveAsync(batch);

                _chaosKitty.Meow(command.BatchId);
            }

            if (transitionResult.ShouldPublishEvents())
            {
                publisher.PublishEvent
                (
                    new BatchClosedEvent
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
