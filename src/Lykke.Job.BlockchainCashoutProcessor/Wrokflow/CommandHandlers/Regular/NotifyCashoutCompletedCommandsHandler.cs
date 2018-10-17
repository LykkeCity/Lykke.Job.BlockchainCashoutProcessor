using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Regular;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Regular
{
    public class NotifyCashoutCompletedCommandsHandler
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
                OperationId = command.OperationId,
                TransactionHash = command.TransactionHash,
                StartMoment = command.StartMoment,
                FinishMoment = command.FinishMoment
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
