using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Core.Repositories;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class AddOperationToBatchCommandHandler
    {
        private readonly IActiveBatchRepository _activeBatchRepository;
        private readonly IChaosKitty _chaosKitty;
        private readonly RetryDelayProvider _retryDelayProvider;

        public AddOperationToBatchCommandHandler(IActiveBatchRepository activeBatchRepository, 
            IChaosKitty chaosKitty, RetryDelayProvider retryDelayProvider)
        {
            _activeBatchRepository = activeBatchRepository;
            _chaosKitty = chaosKitty;
            _retryDelayProvider = retryDelayProvider;
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

            //wait for active batch to disposed and recreate new one
            if (activeBatch.IsClosed)
            {
                return CommandHandlingResult.Fail(_retryDelayProvider.DefaultRetryDelay);
            }

            activeBatch.AddOperation(command.OperationId, command.ToAddress, command.Amount);

            await _activeBatchRepository.SaveAsync(activeBatch);

            return CommandHandlingResult.Ok();
        }
    }
}
