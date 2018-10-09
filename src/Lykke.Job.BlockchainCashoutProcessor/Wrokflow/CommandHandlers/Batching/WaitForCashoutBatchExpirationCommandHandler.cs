using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings;
using Lykke.Job.BlockchainCashoutProcessor.StateMachine;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batching
{
    [UsedImplicitly]
    public class WaitForCashoutBatchExpirationsCommandHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly BatchMonitoringSettings _monitoringSettings;
        private readonly ICashoutsBatchRepository _cashoutsBatchRepository;

        public WaitForCashoutBatchExpirationsCommandHandler(
            IChaosKitty chaosKitty,
            BatchMonitoringSettings monitoringSettings,
            ICashoutsBatchRepository cashoutsBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _monitoringSettings = monitoringSettings;
            _cashoutsBatchRepository = cashoutsBatchRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(WaitForCashoutBatchExpirationCommand command, IEventPublisher publisher)
        {
            var batch = await _cashoutsBatchRepository.GetAsync(command.BatchId);

            if (!batch.IsStillFilledUp)
            {
                return CommandHandlingResult.Ok();
            }

            if (!batch.IsExpired)
            {
                return CommandHandlingResult.Fail(_monitoringSettings.Period);
            }

            var transitionResult = batch.Expire();

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashoutsBatchRepository.SaveAsync(batch);

                _chaosKitty.Meow(command.BatchId);
            }

            if (transitionResult.ShouldPublishEvents())
            {
                publisher.PublishEvent
                (
                    new BatchExpiredEvent
                    {
                        BatchId = batch.BatchId
                    }
                );

                _chaosKitty.Meow(command.BatchId);
            }

            return CommandHandlingResult.Ok();
        }
    }
}
