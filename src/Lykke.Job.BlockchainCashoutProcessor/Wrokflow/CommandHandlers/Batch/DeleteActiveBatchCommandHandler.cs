using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Repositories;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batch;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batch
{
    public class DeleteActiveBatchCommandHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly IActiveBatchRepository _activeBatchRepository;

        public DeleteActiveBatchCommandHandler(IChaosKitty chaosKitty,
            IActiveBatchRepository activeBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _activeBatchRepository = activeBatchRepository;
        }
        
        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(DeleteActiveBatchCommand command, IEventPublisher publisher)
        {
            _chaosKitty.Meow(command.BatchId);

            await _activeBatchRepository.DeleteIfExistAsync(command.BlockchainType, command.HotWalletAddress,
                command.BlockchainAssetId, command.BatchId);

            return CommandHandlingResult.Ok();
        }
    }
}
