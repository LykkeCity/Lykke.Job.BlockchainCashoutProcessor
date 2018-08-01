using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batch;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batch;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batch;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas
{
    public class CashoutBatchSaga
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ICashoutBatchRepository _cashoutBatchRepository;
        private static string Self => BlockchainCashoutBatchProcessorBoundedContext.Name;

        public CashoutBatchSaga(IChaosKitty chaosKitty, ICashoutBatchRepository cashoutBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _cashoutBatchRepository = cashoutBatchRepository;
        }
        
        [UsedImplicitly]
        private async Task Handle(CashoutBatchStartedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutBatchRepository.GetOrAddAsync(evt.BatchId,
                () => CashoutBatchAggregate.CreateNew(evt.BatchId,
                    evt.BlockchainType,
                    evt.BlockchainAssetId,
                    evt.IncludeFee,
                    evt.HotWallet));

            _chaosKitty.Meow(evt.BatchId);

            if (aggregate.OnCashoutBatchStarted())
            {
                await _cashoutBatchRepository.SaveAsync(aggregate);

                sender.SendCommand(new WaitBatchClosingCommand
                {
                    BatchId = evt.BatchId,
                    BlockchainType = evt.BlockchainType,
                    HotWallet = evt.HotWallet
                }, Self);

                _chaosKitty.Meow(evt.BatchId);
            }
        }

        private async Task Handle(CashoutBatchOperationAddedEvent evt,  ICommandSender sender)
        {
            var aggregate = await _cashoutBatchRepository.GetAsync(evt.BatchId);

            if (aggregate.OnCashoutBatchOperationAdded(evt.OperationId, 
                evt.OperationAmount,
                evt.OperationDestinationAddress))
            {
                sender.SendCommand(new NotifyCashoutSuccessfullyAddedToBatchCommand
                {
                    BatchId = aggregate.BatchId,
                    OperationId = evt.OperationId
                }, BlockchainCashoutProcessorBoundedContext.Name);

                _chaosKitty.Meow(evt.BatchId);

                await _cashoutBatchRepository.SaveAsync(aggregate);
            }
            else
            {
                _chaosKitty.Meow(evt.BatchId);

                sender.SendCommand(new NotifyCashoutFailedToAddToBatchCommand
                {
                    OperationId = evt.OperationId
                }, BlockchainCashoutProcessorBoundedContext.Name);
            }
        }

        [UsedImplicitly]
        private async Task Handle(CashoutBatchClosedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutBatchRepository.GetAsync(evt.BatchId);

            _chaosKitty.Meow(evt.BatchId);

            if (aggregate.OnCashoutBatchClosed())
            {
                sender.SendCommand(new BlockchainOperationsExecutor.Contract.Commands.StartOneToManyOutputsExecutionCommand
                    {
                        OperationId = aggregate.BatchId,
                        AssetId = aggregate.BlockchainAssetId,
                        FromAddress = aggregate.HotWalletAddress,
                        IncludeFee = aggregate.IncludeFee,
                        To = aggregate.ToOperations.Select(p => (p.destinationAddress, p.amount)).ToArray()
                    }, BlockchainOperationsExecutorBoundedContext.Name);
                
                _chaosKitty.Meow(evt.BatchId);

                await _cashoutBatchRepository.SaveAsync(aggregate);
            }
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutBatchRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashout batch operation
                return;
            }

            if (aggregate.OnBatchCompeted())
            {
                sender.SendCommand(new NotifyBatchCompletedCommand
                {
                    Block = evt.Block,
                    Fee = evt.Fee,
                    BatchId = aggregate.BatchId,
                    ToOperations = aggregate.ToOperations
                }, Self);

                _chaosKitty.Meow(evt.OperationId);

                await _cashoutBatchRepository.SaveAsync(aggregate);
            }
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutBatchRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashout batch  operation
                return;
            }

            if (aggregate.OnBatchFailed())
            {
                sender.SendCommand(new NotifyBatchFailedCommand
                {
                    BatchId = aggregate.BatchId,
                    ToOperations = aggregate.ToOperations,
                    Error = evt.Error,
                    ErrorCode = evt.ErrorCode
                }, Self);

                _chaosKitty.Meow(evt.OperationId);

                await _cashoutBatchRepository.SaveAsync(aggregate);
            }
        }
    }
}
