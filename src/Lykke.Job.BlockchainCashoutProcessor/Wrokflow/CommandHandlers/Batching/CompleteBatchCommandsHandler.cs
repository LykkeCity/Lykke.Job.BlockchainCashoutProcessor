using System;
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
    public class CompleteBatchCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ICashoutsBatchRepository _cashoutsBatchRepository;

        public CompleteBatchCommandsHandler(
            IChaosKitty chaosKitty,
            ICashoutsBatchRepository cashoutsBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _cashoutsBatchRepository = cashoutsBatchRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(CompleteBatchCommand command, IEventPublisher publisher)
        {
            var batch = await _cashoutsBatchRepository.GetAsync(command.BatchId);

            var transitionResult = batch.Complete();

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashoutsBatchRepository.SaveAsync(batch);

                _chaosKitty.Meow(batch.BatchId);
            }

            if (transitionResult.ShouldPublishEvents())
            {
                if (!batch.FinishMoment.HasValue)
                {
                    throw new InvalidOperationException("Finish moment should be not null here");
                }

                publisher.PublishEvent
                (
                    new CashoutsBatchCompletedEvent
                    {
                        BatchId = batch.BatchId,
                        AssetId = batch.AssetId,
                        TransactionHash = command.TransactionHash,
                        TransactionFee = command.TransactionFee,
                        Cashouts = batch.Cashouts
                            .Select(c => c.ToContract())
                            .ToArray(),
                        StartMoment = batch.StartMoment,
                        FinishMoment = batch.FinishMoment.Value
                    }
                );

                _chaosKitty.Meow(batch.BatchId);
            }

            return CommandHandlingResult.Ok();
        }
    }
}
