using System.Threading.Tasks;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    public class StartBatchExecutionCommandHandler
    {
        private readonly IChaosKitty _chaosKitty;

        public StartBatchExecutionCommandHandler(IChaosKitty chaosKitty)
        {
            _chaosKitty = chaosKitty;
        }

        public Task<CommandHandlingResult> Handle(StartBatchExecutionCommand command, 
            IEventPublisher publisher)
        {
            publisher.PublishEvent(new BatchExecutionStartedEvent
            {
                BatchId = command.BatchId,
                BlockchainType = command.BlockchainType,
                StartedAt = command.StartedAt,
                BlockchainAssetId = command.BlockchainAssetId,
                HotWallet = command.HotWallet,
                IncludeFee = false
            });

            _chaosKitty.Meow(command.BatchId);

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
