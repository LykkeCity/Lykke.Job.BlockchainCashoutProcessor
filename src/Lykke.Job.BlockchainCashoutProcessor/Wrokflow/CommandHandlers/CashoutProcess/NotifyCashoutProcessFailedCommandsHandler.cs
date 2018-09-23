using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    public class NotifyCashoutFailedCommandsHandler
    {
        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyCashoutFailedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent(new CashoutFailedEvent()
            {
                ClientId = command.ClientId,
                AssetId = command.AssetId,
                Amount = command.Amount,
                OperationId = command.OperationId,
                Error = command.Error,
                ErrorCode = command.ErrorCode,
                FinishMoment = command.FinishMoment,
                StartMoment = command.StartMoment
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
