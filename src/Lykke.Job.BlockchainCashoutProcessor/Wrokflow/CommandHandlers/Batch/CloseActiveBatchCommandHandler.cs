using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Repositories;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batch;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batch;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batch
{
    public class CloseActiveBatchCommandHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly IActiveBatchRepository _activeBatchRepository;

        public CloseActiveBatchCommandHandler(IChaosKitty chaosKitty,
            IActiveBatchRepository activeBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _activeBatchRepository = activeBatchRepository;
        }
        
        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(CloseActiveBatchCommand command, IEventPublisher publisher)
        {
            var activeBatch = await _activeBatchRepository.TryGetAsync(command.BlockchainType,
                command.HotWalletAddress,
                command.BlockchainAssetId,
                command.BatchId);
            
            //check batch already deleted
            if (activeBatch != null)
            {
                activeBatch.Close();

                await _activeBatchRepository.SaveAsync(activeBatch);

                _chaosKitty.Meow(command.BatchId);

                publisher.PublishEvent(new BatchClosedEvent
                {
                    BatchId = activeBatch.BatchId,
                    Operations = activeBatch.Operations
                });
            }

            return CommandHandlingResult.Ok();
        }
    }
}
