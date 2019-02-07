using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;
using MoreLinq;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas
{
    public class BatchSaga
    {
        private static string Self => BlockchainCashoutProcessorBoundedContext.Name;

        private readonly IChaosKitty _chaosKitty;
        private readonly ICashoutsBatchReadOnlyRepository _cashoutsBatchRepository;

        public BatchSaga(
            IChaosKitty chaosKitty,
            ICashoutsBatchReadOnlyRepository cashoutsBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _cashoutsBatchRepository = cashoutsBatchRepository;
        }

        [UsedImplicitly]
        private Task Handle(BatchFillingStartedEvent evt, ICommandSender sender)
        {
            sender.SendCommand
            (
                new WaitForBatchExpirationCommand
                {
                    BatchId = evt.BatchId
                },
                Self
            );

            _chaosKitty.Meow(evt.BatchId);

            return Task.CompletedTask;
        }

        [UsedImplicitly]
        private Task Handle(BatchFilledEvent evt, ICommandSender sender)
        {
            sender.SendCommand
            (
                new CloseBatchCommand
                {
                    BatchId = evt.BatchId,
                    Reason = CashoutsBatchClosingReason.Filled
                },
                Self
            );

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
                    Reason = CashoutsBatchClosingReason.Expired
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
        public async Task Handle(ActiveBatchIdRevokedEvent evt, ICommandSender sender)
        {
            var batch = await _cashoutsBatchRepository.GetAsync(evt.BatchId);
            var outputs = batch.Cashouts
                .GroupBy(x => x.ToAddress)
                .Select(x =>
                {
                    return new BlockchainOperationsExecutor.Contract.OperationOutput
                    {
                        Address = x.Key,
                        Amount = x.Sum(y => y.Amount)
                    };
                }).ToArray();

            sender.SendCommand
            (
                new BlockchainOperationsExecutor.Contract.Commands.StartOneToManyOutputsExecutionCommand
                {
                    OperationId = batch.BatchId,
                    AssetId = batch.AssetId,
                    FromAddress = batch.HotWalletAddress,
                    // For the cashout all amount should be transfered to the destination address,
                    // so the fee shouldn't be included in the amount.
                    IncludeFee = false,
                    Outputs = outputs
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
                new CompleteBatchCommand
                {
                    BatchId = batch.BatchId,
                    TransactionFee = evt.Fee,
                    TransactionHash = evt.TransactionHash
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
                new FailBatchCommand
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
