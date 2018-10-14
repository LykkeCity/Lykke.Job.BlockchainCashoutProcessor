using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.StateMachine;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batching
{
    [UsedImplicitly]
    public class WaitForBatchExpirationCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly TimeSpan _batchExpirationMonitoringPeriod;
        private readonly ICashoutsBatchRepository _cashoutsBatchRepository;

        public WaitForBatchExpirationCommandsHandler(
            IChaosKitty chaosKitty,
            TimeSpan batchExpirationMonitoringPeriod,
            ICashoutsBatchRepository cashoutsBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _batchExpirationMonitoringPeriod = batchExpirationMonitoringPeriod;
            _cashoutsBatchRepository = cashoutsBatchRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(WaitForBatchExpirationCommand command, IEventPublisher publisher)
        {
            var batch = await _cashoutsBatchRepository.GetAsync(command.BatchId);

            if (!batch.IsStillFillingUp)
            {
                return CommandHandlingResult.Ok();
            }

            if (!batch.HaveToBeExpired)
            {
                return CommandHandlingResult.Fail(_batchExpirationMonitoringPeriod);
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
