using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class OperationCompletedCommandsHandler
    {
        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyCashoutCompletedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent(new CashoutCompletedEvent()
            {
                ToAddress = command.ToAddress,
                AssetId = command.AssetId,
                ClientId = command.ClientId,
                Amount = command.Amount,
                TransactionHash = command.TransactionHash
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }

        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyCashinCompletedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent(new CashinCompletedEvent()
            {
                ClientId = command.ClientId ,
                AssetId =  command.AssetId,
                Amount = command.Amount
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
