using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class AddOperationToBatchCommandHandler
    {
        private readonly IActiveBatchRepository _activeBatchRepository;
        private readonly IChaosKitty _chaosKitty;

        public AddOperationToBatchCommandHandler(IActiveBatchRepository activeBatchRepository, 
            IChaosKitty chaosKitty)
        {
            _activeBatchRepository = activeBatchRepository;
            _chaosKitty = chaosKitty;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(AddOperationToBatchCommand command, IEventPublisher publisher)
        {
            var activeBatch = await _activeBatchRepository.GetOrAddAsync(command.BlockchainType, command.HotWalletAddress,
                command.BlockchainAssetId,
                () => ActiveBatchAggregate.StartNew(command.BlockchainType, 
                    command.BlockchainAssetId,
                    command.HotWalletAddress));

            _chaosKitty.Meow(command.OperationId);

            activeBatch.AddOperation(command.OperationId, command.ToAddress, command.Amount);

            await _activeBatchRepository.SaveAsync(activeBatch);

            _chaosKitty.Meow(command.OperationId);

            return CommandHandlingResult.Ok();
        }
    }
}
