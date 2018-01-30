using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
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
    ///     if  -> OperationCompletedEvent
    ///     {
    ///             -> RegisterClientOperationFinishCommand
    ///         -> ClientOperationFinishRegisteredEvent
    ///     }
    /// </summary>
    [UsedImplicitly]
    public class CashoutSaga
    {
        private static readonly string Self = BlockchainOperationsExecutorBoundedContext.Name;

        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly ICashoutRepository _cashoutRepository;

        public CashoutSaga(
            IChaosKitty chaosKitty,
            ILog log, 
            ICashoutRepository cashoutRepository)
        {
            _chaosKitty = chaosKitty;
            _log = log;
            _cashoutRepository = cashoutRepository;
        }

        [UsedImplicitly]
        private async Task Handle(CashoutStartedEvent evt, ICommandSender sender)
        {
#if DEBUG
            _log.WriteInfo(nameof(CashoutStartedEvent), evt, "");
#endif

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
#if DEBUG
            _log.WriteInfo(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, "");
#endif
            try
            {
                var aggregate = await _cashoutRepository.TryGetAsync(evt.OperationId);
                
                if (aggregate == null)
                {
                    // This is not a cashout operation
                    return;
                }

                if (aggregate.OnOperationCompleted(evt.TransactionHash, evt.TransactionAmount, evt.Fee))
                {
                    sender.SendCommand(new RegisterClientOperationFinishCommand
                    {
                        OperationId = aggregate.OperationId,
                        TransactionHash = aggregate.TransactionHash,
                        ClientId = aggregate.ClientId
                    }, Self);

                    _chaosKitty.Meow(evt.OperationId);

                    await _cashoutRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent evt, ICommandSender sender)
        {
#if DEBUG
            _log.WriteInfo(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent), evt, "");
#endif
            try
            {
                var aggregate = await _cashoutRepository.TryGetAsync(evt.OperationId);

                if (aggregate == null)
                {
                    // This is not a cashout operation
                    return;
                }

                if (aggregate.OnOperationFailed(evt.Error))
                {
                    await _cashoutRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(ClientOperationFinishRegisteredEvent evt, ICommandSender sender)
        {
#if DEBUG
            _log.WriteInfo(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent), evt, "");
#endif
            try
            {
                var aggregate = await _cashoutRepository.GetAsync(evt.OperationId);

                if (aggregate.OnClientOperationFinishRegisteredEvent())
                {
                    await _cashoutRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent), evt, ex);
                throw;
            }
        }
    }
}
