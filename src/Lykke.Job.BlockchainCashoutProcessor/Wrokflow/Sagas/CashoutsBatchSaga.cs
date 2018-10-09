using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas
{
    public class CashoutsBatchSaga
    {
        private static string Self => BlockchainCashoutProcessorBoundedContext.Name;

        private readonly IChaosKitty _chaosKitty;
        private readonly ICashoutsBatchReadOnlyRepository _cashoutsBatchRepository;

        public CashoutsBatchSaga(
            IChaosKitty chaosKitty, 
            ICashoutsBatchReadOnlyRepository cashoutsBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _cashoutsBatchRepository = cashoutsBatchRepository;
        }

        [UsedImplicitly]
        private Task Handle(CashoutAddedToBatchEvent evt, ICommandSender sender)
        {
            if (evt.CashoutsCount == 1)
            {
                sender.SendCommand
                (
                    new WaitForCashoutBatchExpirationCommand
                    {
                        BatchId = evt.BatchId
                    },
                    Self
                );
            }
            else if (evt.CashoutsCount > evt.CashoutsCountThreshold)
            {
                sender.SendCommand
                (
                    new CloseBatchCommand
                    {
                        BatchId = evt.BatchId,
                        Reason = CashoutBatchClosingReason.CashoutsCountExceeded
                    },
                    Self
                );
            }

            _chaosKitty.Meow(evt.BatchId);

            return Task.CompletedTask;
        }

        [UsedImplicitly]
        private Task Handle(BatchExpiredEvent evt, ICommandSender sender)
        {
            sender.SendCommand
            (
                new CloseBatchCommand
                {
                    BatchId = evt.BatchId,
                    Reason = CashoutBatchClosingReason.Expired
                },
                Self
            );

            _chaosKitty.Meow(evt.BatchId);

            return Task.CompletedTask;
        }

        [UsedImplicitly]
        private Task Handle(BatchClosedEvent evt, ICommandSender sender)
        {
            sender.SendCommand
            (
                new RevokeActiveBatchIdCommand
                {
                    BatchId = evt.BatchId
                },
                Self
            );

            _chaosKitty.Meow(evt.BatchId);

            return Task.CompletedTask;
        }

        [UsedImplicitly]
        private async Task Handle(BatchIdRevokedEvent evt, ICommandSender sender)
        {
            var batch = await _cashoutsBatchRepository.GetAsync(evt.BatchId);

            sender.SendCommand
            (
                new BlockchainOperationsExecutor.Contract.Commands.StartOneToManyOutputsExecutionCommand
                {
                    OperationId = batch.BatchId,
                    AssetId = batch.AssetId,
                    FromAddress = batch.HotWalletAddress,
                    IncludeFee = true,
                    Outputs = batch.Cashouts
                        .Select(c => new BlockchainOperationsExecutor.Contract.OperationOutput
                        {
                            Address = c.ToAddress,
                            Amount = c.Amount
                        })
                        .ToArray()
                },
                BlockchainOperationsExecutor.Contract.BlockchainOperationsExecutorBoundedContext.Name
            );

            _chaosKitty.Meow(evt.BatchId);
        }
        
        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OneToManyOperationExecutionCompletedEvent evt, ICommandSender sender)
        {
            var batch = await _cashoutsBatchRepository.TryGetAsync(evt.OperationId);

            if (batch == null)
            {
                return;
            }

            sender.SendCommand
            (
                new CompleteCashoutsBatchCommand
                {
                    BatchId = batch.BatchId
                },
                Self
            );

            _chaosKitty.Meow(evt.OperationId);
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent evt, ICommandSender sender)
        {
            var batch = await _cashoutsBatchRepository.TryGetAsync(evt.OperationId);

            if (batch == null)
            {
                return;
            }

            sender.SendCommand
            (
                new FailCashoutsBatchCommand
                {
                    BatchId = batch.BatchId,
                    ErrorCode = evt.ErrorCode,
                    Error = evt.Error
                },
                Self
            );

            _chaosKitty.Meow(evt.OperationId);
        }
    }
}
