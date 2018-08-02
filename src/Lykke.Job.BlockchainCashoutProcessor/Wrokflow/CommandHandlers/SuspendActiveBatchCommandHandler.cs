using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Repositories;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    public class SuspendActiveBatchCommandHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly IActiveBatchRepository _activeBatchRepository;

        public SuspendActiveBatchCommandHandler(IChaosKitty chaosKitty,
            IActiveBatchRepository activeBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _activeBatchRepository = activeBatchRepository;
        }
        
        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(SuspendActiveBatchCommand command, IEventPublisher publisher)
        {
            var activeBatch = await _activeBatchRepository.TryGetAsync(command.BlockchainType,
                command.HotWalletAddress,
                command.BlockchainAssetId,
                command.BatchId);
            
            //check batch already disposed
            if (activeBatch != null)
            {
                activeBatch.Suspend();

                await _activeBatchRepository.SaveAsync(activeBatch);

                _chaosKitty.Meow(command.BatchId);

                publisher.PublishEvent(new BatchSuspendedEvent
                {
                    BatchId = activeBatch.BatchId,
                    Operations = activeBatch.Operations
                });
            }

            return CommandHandlingResult.Ok();
        }
    }
}
