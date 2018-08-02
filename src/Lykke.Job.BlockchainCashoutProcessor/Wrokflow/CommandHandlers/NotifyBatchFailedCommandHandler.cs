using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    public class NotifyBatchFailedCommandHandler
    {
        private readonly IChaosKitty _chaosKitty;

        public NotifyBatchFailedCommandHandler(IChaosKitty chaosKitty)
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

                publisher.PublishEvent(new BatchedOperationExecutionFailedEvent
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
