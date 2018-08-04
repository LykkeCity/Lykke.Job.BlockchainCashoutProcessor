using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batch;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas
{
    public class CashoutBatchSaga
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ICashoutBatchRepository _cashoutBatchRepository;
        private static string Self => BlockchainCashoutProcessorBoundedContext.Name;

        public CashoutBatchSaga(IChaosKitty chaosKitty, ICashoutBatchRepository cashoutBatchRepository)
        {
            _chaosKitty = chaosKitty;
            _cashoutBatchRepository = cashoutBatchRepository;
        }
        
        [UsedImplicitly]
        private async Task Handle(BatchExecutionStartedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutBatchRepository.GetOrAddAsync(evt.BatchId,
                () => CashoutBatchAggregate.CreateNew(evt.BatchId,
                    evt.BlockchainType,
                    evt.BlockchainAssetId,
                    evt.IncludeFee,
                    evt.HotWallet,
                    evt.StartedAt));

            _chaosKitty.Meow(evt.BatchId);

            if (aggregate.OnBatchStarted())
            {
                sender.SendCommand(new SuspendActiveBatchCommand
                    {
                        BatchId = aggregate.BatchId,
                        BlockchainType = aggregate.BlockchainType,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        HotWalletAddress = aggregate.HotWalletAddress
                    }, 
                    Self);

                _chaosKitty.Meow(evt.BatchId);

                await _cashoutBatchRepository.SaveAsync(aggregate);
            }
        }

        [UsedImplicitly]
        private async Task Handle(BatchSuspendedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutBatchRepository.GetAsync(evt.BatchId);

            _chaosKitty.Meow(evt.BatchId);

            if (aggregate.OnBatchSuspended(evt.Operations))
            {
                sender.SendCommand(new BlockchainOperationsExecutor.Contract.Commands.StartOneToManyOutputsExecutionCommand
                {
                    OperationId = aggregate.BatchId,
                    AssetId = aggregate.BlockchainAssetId,
                    FromAddress = aggregate.HotWalletAddress,
                    IncludeFee = aggregate.IncludeFee,
                    To = aggregate.ToOperations
                        .Select(p => new BlockchainOperationsExecutor.Contract.Commands.OperationOutputContract { Address = p.destinationAddress, Amount = p.amount} )
                        .ToArray(),
                    OperationIdsFromBatch = aggregate.ToOperations
                        .Select(p => p.operationId)
                        .ToArray()
                }, BlockchainOperationsExecutorBoundedContext.Name);

                _chaosKitty.Meow(evt.BatchId);

                sender.SendCommand(new DeleteActiveBatchCommand
                    {
                        BatchId = aggregate.BatchId,
                        BlockchainType = aggregate.BlockchainType,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        HotWalletAddress = aggregate.HotWalletAddress
                    },
                    Self);

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
                await _cashoutBatchRepository.SaveAsync(aggregate);
                
                _chaosKitty.Meow(evt.OperationId);
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
                await _cashoutBatchRepository.SaveAsync(aggregate);

                _chaosKitty.Meow(evt.OperationId);
            }
        }
    }
}
