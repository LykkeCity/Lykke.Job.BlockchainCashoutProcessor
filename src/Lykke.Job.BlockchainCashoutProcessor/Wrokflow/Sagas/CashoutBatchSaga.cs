using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.ContractMapping;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
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

            var cashouts = evt.Cashouts
                .Select(BatchedCashoutMappingExtensions.ToDmoain)
                .ToArray();

            if (aggregate.OnBatchSuspended(cashouts))
            {
                sender.SendCommand(
                    new BlockchainOperationsExecutor.Contract.Commands.StartOneToManyOutputsExecutionCommand
                    {
                        OperationId = aggregate.BatchId,
                        AssetId = aggregate.BlockchainAssetId,
                        FromAddress = aggregate.HotWalletAddress,
                        IncludeFee = aggregate.IncludeFee,
                        Outputs = aggregate.Cashouts
                            .Select(x => new OperationOutput
                            {
                                Address = x.DestinationAddress,
                                Amount = x.Amount
                            })
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
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OneToManyOperationExecutionCompletedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashoutBatchRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashout batch operation
                return;
            }

            if (aggregate.OnBatchCompeted(
                evt.TransactionHash, 
                evt.TransactionOutputs
                    .Select(x => x.ToDomain())
                    .ToArray(), 
                evt.Fee, 
                evt.Block))
            {
                await _cashoutBatchRepository.SaveAsync(aggregate);

                sender.SendCommand
                (
                    new NotifyCashoutBatchCompletedCommand
                    {
                        BatchId = aggregate.BatchId,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        TransactionOutput = aggregate.TransactionOutputs
                            .Select(x => x.ToContract())
                            .ToArray(),
                        TransactionHash = aggregate.TransactionHash
                    },
                    BlockchainCashoutProcessorBoundedContext.Name
                );
                

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
