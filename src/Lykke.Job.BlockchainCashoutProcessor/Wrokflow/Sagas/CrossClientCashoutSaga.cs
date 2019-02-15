using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Modules;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.CrossClient;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas
{
    /// <summary>
    /// -> RiskControlSaga : CrossClientCashoutStartedEvent
    ///     -> EnrollToMatchingEngineCommand
    /// -> CashinEnrolledToMatchingEngineEvent
    /// </summary>
    [UsedImplicitly]
    public class CrossClientCashoutSaga
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ICrossClientCashoutRepository _cashoutRepository;

        public CrossClientCashoutSaga(
            IChaosKitty chaosKitty,
            ICrossClientCashoutRepository cashoutRepository)
        {
            _chaosKitty = chaosKitty;
            _cashoutRepository = cashoutRepository;
        }

        [UsedImplicitly]
        private async Task Handle(CrossClientCashoutStartedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutRepository.GetOrAddAsync(
                evt.OperationId,
                () => CrossClientCashoutAggregate.Start(
                    evt.OperationId,
                    evt.ClientId,
                    evt.BlockchainType,
                    evt.BlockchainAssetId,
                    evt.HotWalletAddress,
                    evt.ToAddress,
                    evt.Amount,
                    evt.AssetId,
                    evt.RecipientClientId));

            _chaosKitty.Meow(evt.OperationId);

            if (aggregate.State == CrossClientCashoutState.Started)
            {
                sender.SendCommand
                (
                    new EnrollToMatchingEngineCommand
                    {
                        CashinOperationId = aggregate.CashinOperationId,
                        CashoutOperationId = aggregate.OperationId,
                        RecipientClientId = aggregate.RecipientClientId,
                        Amount = aggregate.Amount,
                        AssetId = aggregate.AssetId
                    },
                    CqrsModule.Self
                );

                _chaosKitty.Meow(evt.OperationId);
            }
        }

        [UsedImplicitly]
        private async Task Handle(CashinEnrolledToMatchingEngineEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutRepository.GetAsync(evt.CashoutOperationId);

            var matchingEngineEnrollementMoment = DateTime.UtcNow;

            if (aggregate.OnEnrolledToMatchingEngine(matchingEngineEnrollementMoment))
            {
                if (!aggregate.MatchingEngineEnrollementMoment.HasValue)
                {
                    throw new InvalidOperationException("ME enrollement moment should be not null here");
                }

                sender.SendCommand
                (
                    new NotifyCrossClientCashoutCompletedCommand
                    {
                        OperationId = aggregate.OperationId,
                        ClientId = aggregate.ClientId,
                        CashinOperationId = aggregate.CashinOperationId,
                        RecipientClientId = aggregate.RecipientClientId,
                        AssetId = aggregate.AssetId,
                        Amount = aggregate.Amount,
                        StartMoment = aggregate.StartMoment,
                        FinishMoment = aggregate.MatchingEngineEnrollementMoment.Value
                    },
                    BlockchainCashoutProcessorBoundedContext.Name
                );

                _chaosKitty.Meow(evt.CashoutOperationId);

                await _cashoutRepository.SaveAsync(aggregate);
            }
        }
    }
}
