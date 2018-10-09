using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class NotifyOpetationFinishedCommandsHandler
    {
        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyCashoutCompletedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent(new CashoutCompletedEvent
            {
                ToAddress = command.ToAddress,
                Amount = command.Amount,
                AssetId = command.AssetId,
                ClientId = command.ClientId,
                TransactionAmount = command.TransactionAmount,
                TransactionFee = command.TransactionFee,
                OperationType = command.OperationType,
                OperationId = command.OperationId,
                TransactionHash = command.TransactionHash,
                StartMoment = command.StartMoment,
                FinishMoment = command.FinishMoment
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }

        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyCrossClientCashinCompletedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent
            (
                new CrossClientCashinCompletedEvent
                {
                    OperationId = command.OperationId,
                    CashoutOperationId = command.CashoutOperationId,
                    ClientId = command.ClientId,
                    SenderClientId = command.SenderClientId,
                    AssetId = command.AssetId,
                    Amount = command.Amount,
                    TransactionAmount = command.TransactionAmount,
                    TransactionFee = command.TransactionFee
                }
            );

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
