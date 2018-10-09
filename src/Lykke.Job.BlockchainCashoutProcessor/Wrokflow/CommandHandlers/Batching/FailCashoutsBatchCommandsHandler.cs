using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.ContractMapping;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.StateMachine;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batching
{
    [UsedImplicitly]
    public class FailCashoutsBatchCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ICashoutsBatchRepository _cashoutsBatchRepository;

        public FailCashoutsBatchCommandsHandler(
            IChaosKitty chaosKitty,
            ICashoutsBatchRepository cashoutsBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _cashoutsBatchRepository = cashoutsBatchRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(FailCashoutsBatchCommand command, IEventPublisher publisher)
        {
            var batch = await _cashoutsBatchRepository.GetAsync(command.BatchId);

            var transitionResult = batch.Fail();

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashoutsBatchRepository.SaveAsync(batch);

                _chaosKitty.Meow(batch.BatchId);
            }

            if (transitionResult.ShouldPublishEvents())
            {
                publisher.PublishEvent
                (
                    new CashoutsBatchFailedEvent
                    {
                        BatchId = batch.BatchId,
                        AssetId = batch.AssetId,
                        Cashouts = batch.Cashouts
                            .Select(c => c.ToContract())
                            .ToArray(),
                        ErrorCode = command.ErrorCode.ToContract(),
                        Error = command.Error
                    }
                );
            }

            return CommandHandlingResult.Ok();
        }
    }
}
