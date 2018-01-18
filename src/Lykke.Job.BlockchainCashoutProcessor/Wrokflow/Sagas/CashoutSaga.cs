using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas
{
    /// <summary>
    /// -> Lykke.Job.TransactionsHandler : StartCashoutCommand
    /// -> CashoutStartedEvent
    ///     -> BlockchainOperationsExecutor : StartOperationCommand
    /// </summary>
    [UsedImplicitly]
    public class CashoutSaga
    {
        [UsedImplicitly]
        private Task Handle(CashoutStartedEvent evt, ICommandSender sender)
        {
            // Since this saga is so simple at the moment, there is no needed to
            // keep the aggregate, but consider to add the aggregate, if saga will became
            // more complex

            sender.SendCommand(new BlockchainOperationsExecutor.Contract.Commands.StartOperationExecutionCommand
            {
                OperationId = evt.OperationId,
                FromAddress = evt.HotWalletAddress,
                ToAddress = evt.ToAddress,
                AssetId = evt.AssetId,
                Amount = evt.Amount,
                // For the cashout all amount should be transfered to the destination address,
                // so the fee shouldn't be included in the amount.
                IncludeFee = false
            }, BlockchainOperationsExecutorBoundedContext.Name);

            return Task.CompletedTask;
        }
    }
}
