using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
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

        [UsedImplicitly]
        private async Task Handle(CashinEnrolledToMatchingEngineEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutRepository.GetAsync(evt.CashoutOperationId);

            var matchingEngineEnrollementMoment = DateTime.UtcNow;

            if (aggregate.OnEnrolledToMatchingEngine(matchingEngineEnrollementMoment))
            {
                _chaosKitty.Meow(evt.CashoutOperationId);

                await _cashoutRepository.SaveAsync(aggregate);
            }

            sender.SendCommand(new NotifyCashoutCompletedCommand
                {
                    Amount = aggregate.Amount,
                    TransactionAmount = 0M,
                    TransactionFee = 0M,
                    AssetId = aggregate.AssetId,
                    ClientId = aggregate.ClientId,
                    ToAddress = aggregate.ToAddress,
                    OperationType = CashoutOperationType.OffBlockchain,
                    OperationId = aggregate.OperationId,
                    TransactionHash = "0x",
                    StartMoment = aggregate.StartMoment,
                    FinishMoment = matchingEngineEnrollementMoment
            },
                BlockchainCashoutProcessorBoundedContext.Name);

            sender.SendCommand(new NotifyCashinCompletedCommand()
            {
                AssetId = aggregate.AssetId,
                Amount = aggregate.Amount,
                TransactionAmount = 0M,
                TransactionFee = 0M,
                ClientId = aggregate.RecipientClientId,
                OperationId = aggregate.CashinOperationId,
                TransactionHash = "0x"
            }
            , BlockchainCashoutProcessorBoundedContext.Name);
        }
    }
}
