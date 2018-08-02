using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batch;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Events;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batch
{
    public class NotifyBatchCompletedCommandHandler
    {
        private readonly IChaosKitty _chaosKitty;

        public NotifyBatchCompletedCommandHandler(IChaosKitty chaosKitty)
        {
            _chaosKitty = chaosKitty;
        }

        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyBatchFailedCommand command,
            IEventPublisher publisher)
        {
            foreach (var op in command.ToOperations)
            {
                _chaosKitty.Meow(op.operationId);

                publisher.PublishEvent(new OperationExecutionFailedEvent
                {
                    OperationId = op.operationId,
                    ErrorCode = command.ErrorCode,
                    Error = command.Error
                });
            }

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
