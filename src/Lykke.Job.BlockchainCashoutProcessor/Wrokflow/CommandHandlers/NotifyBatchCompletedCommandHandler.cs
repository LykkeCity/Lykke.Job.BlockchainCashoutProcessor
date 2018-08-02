using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Events;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    public class NotifyBatchCompetedCommandHandler
    {
        private readonly IChaosKitty _chaosKitty;

        public NotifyBatchCompetedCommandHandler(IChaosKitty chaosKitty)
        {
            _chaosKitty = chaosKitty;
        }

        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyBatchCompletedCommand command,
            IEventPublisher publisher)
        {
            foreach (var op in command.ToOperations)
            {
                _chaosKitty.Meow(op.operationId);

                publisher.PublishEvent(new OperationExecutionCompletedEvent
                {
                    OperationId = op.operationId,
                    Block = command.Block,
                    Fee = command.Fee,
                    TransactionAmount = command.TransactionAmount,
                    TransactionHash = command.TransactionHash
                });
            }

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
