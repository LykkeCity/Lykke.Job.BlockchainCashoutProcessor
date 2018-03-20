using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Modules;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas
{
    /// <summary>
    /// -> Lykke.Job.TransactionsHandler : StartCashoutCommand
    /// -> CrossClientCashoutStartedEvent
    ///     -> EnrollToMatchingEngineCommand
    /// -> CashinEnrolledToMatchingEngineEvent
    /// </summary>
    [UsedImplicitly]
    public class CrossClientCashoutSaga
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly ICrossClientCashoutRepository _cashoutRepository;

        public CrossClientCashoutSaga(
            IChaosKitty chaosKitty,
            ILog log,
            ICrossClientCashoutRepository cashoutRepository)
        {
            _chaosKitty = chaosKitty;
            _log = log;
            _cashoutRepository = cashoutRepository;
        }

        [UsedImplicitly]
        private async Task Handle(CrossClientCashoutStartedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(CrossClientCashoutStartedEvent), evt, "");

            try
            {
                var aggregate = await _cashoutRepository.GetOrAddAsync(
                   evt.OperationId,
                   () => CrossClientCashoutAggregate.StartNewCrossClient(
                       evt.OperationId,
                       evt.FromClientId,
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
                    sender.SendCommand(new EnrollToMatchingEngineCommand
                    {
                        CashinOperationId = aggregate.CashinOperationId,
                        CashoutOperationId = aggregate.OperationId,
                        RecipientClientId = aggregate.RecipientClientId,
                        Amount = aggregate.Amount,
                        AssetId = aggregate.AssetId
                    },
                        CqrsModule.Self);

                    _chaosKitty.Meow(evt.OperationId);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(CrossClientCashoutStartedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(CashinEnrolledToMatchingEngineEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(CashinEnrolledToMatchingEngineEvent), evt, "");

            try
            {
                var aggregate = await _cashoutRepository.GetAsync(evt.CashoutOperationId);

                if (aggregate.OnEnrolledToMatchingEngine())
                {
                    _chaosKitty.Meow(evt.CashoutOperationId);

                    await _cashoutRepository.SaveAsync(aggregate);
                }

                sender.SendCommand(new NotifyCashinCompletedCommand()
                {
                    AssetId = aggregate.AssetId,
                    Amount = aggregate.Amount,
                    ClientId = aggregate.ClientId
                }
                , BlockchainCashoutProcessorBoundedContext.Name);
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(CashinEnrolledToMatchingEngineEvent), evt, ex);
                throw;
            }
        }
    }
}
