using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Mappers;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas
{
    /// <summary>
    /// -> Lykke.Job.TransactionsHandler : StartCashoutCommand
    /// -> CashoutStartedEvent
    ///     -> BlockchainOperationsExecutor : StartOperationCommand
    /// -> BlockchainOperationsExecutor : OperationCompletedEvent | OperationFailedEvent
    /// </summary>
    [UsedImplicitly]
    public class CashoutSaga
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ICashoutRepository _cashoutRepository;

        public CashoutSaga(
            IChaosKitty chaosKitty,
            ICashoutRepository cashoutRepository)
        {
            _chaosKitty = chaosKitty;
            _cashoutRepository = cashoutRepository;
        }

        [UsedImplicitly]
        private async Task Handle(CashoutStartedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutRepository.GetOrAddAsync(
                evt.OperationId,
                () => CashoutAggregate.StartNew(
                    evt.OperationId,
                    evt.ClientId,
                    evt.BlockchainType,
                    evt.BlockchainAssetId,
                    evt.HotWalletAddress,
                    evt.ToAddress,
                    evt.Amount,
                    evt.AssetId));

            _chaosKitty.Meow(evt.OperationId);

            if (aggregate.State == CashoutState.Started)
            {
                // TODO: Add tag (cashin/cashiout) to the operation, and pass it to the operations executor?

                sender.SendCommand(new BlockchainOperationsExecutor.Contract.Commands.StartOperationExecutionCommand
                {
                    OperationId = aggregate.OperationId,
                    FromAddress = aggregate.HotWalletAddress,
                    ToAddress = aggregate.ToAddress,
                    AssetId = aggregate.AssetId,
                    Amount = aggregate.Amount,
                    // For the cashout all amount should be transfered to the destination address,
                    // so the fee shouldn't be included in the amount.
                    IncludeFee = false
                }, BlockchainOperationsExecutorBoundedContext.Name);
            }
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashout operation
                return;
            }

            var operationFinishMoment = DateTime.UtcNow;

            if (aggregate.OnOperationCompleted(evt.TransactionHash, evt.TransactionAmount, evt.Fee, operationFinishMoment))
            {
                await _cashoutRepository.SaveAsync(aggregate);

                _chaosKitty.Meow(evt.OperationId);
            }

            sender.SendCommand(new NotifyCashoutCompletedCommand()
            {
                Amount = aggregate.Amount,
                TransactionAmount = aggregate.TransactionAmount ?? 0M,
                Fee = aggregate.Fee ?? 0M,
                AssetId = aggregate.AssetId,
                ClientId = aggregate.ClientId,
                ToAddress = aggregate.ToAddress,
                OperationType = CashoutOperationType.OnBlockchain,
                OperationId = aggregate.OperationId,
                TransactionHash = aggregate.TransactionHash,
                StartMoment = aggregate.StartMoment,
                FinishMoment = operationFinishMoment
            },
            BlockchainCashoutProcessorBoundedContext.Name);
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashout operation
                return;
            }

            var operationFinishMoment = DateTime.UtcNow;
            if (aggregate.OnOperationFailed(evt.Error, evt.ErrorCode.MapToCashoutErrorCode(), operationFinishMoment))
            {
                if (aggregate.ErrorCode != null)
                {
                    sender.SendCommand(new NotifyCashoutFailedCommand
                        {
                            Amount = aggregate.Amount,
                            AssetId = aggregate.AssetId,
                            ClientId = aggregate.ClientId,
                            OperationId = aggregate.OperationId,
                            Error = aggregate.Error,
                            ErrorCode = aggregate.ErrorCode.Value.MapToCashoutProcessErrorCode(),
                            StartMoment = aggregate.StartMoment,
                            FinishMoment = operationFinishMoment
                        },
                        BlockchainCashoutProcessorBoundedContext.Name
                    );
                }

                await _cashoutRepository.SaveAsync(aggregate);

                _chaosKitty.Meow(evt.OperationId);
            }
        }
    }
}
