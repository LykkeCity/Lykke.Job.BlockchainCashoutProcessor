using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;

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
                TransactionAmount = command.TransactionAmount,
                Fee = command.Fee,
                OperationType = command.OperationType,
                OperationId = command.OperationId,
                TransactionHash = command.TransactionHash,
                StartMoment = command.StartMoment,
                FinishMoment = command.FinishMoment
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }

        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyCashinCompletedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent(new CashinCompletedEvent()
            {
                ClientId = command.ClientId,
                AssetId =  command.AssetId,
                Amount = command.Amount,
                TransactionAmount = command.TransactionAmount,
                Fee = command.Fee,
                OperationType = command.OperationType,
                OperationId = command.OperationId,
                TransactionHash = command.TransactionHash
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
