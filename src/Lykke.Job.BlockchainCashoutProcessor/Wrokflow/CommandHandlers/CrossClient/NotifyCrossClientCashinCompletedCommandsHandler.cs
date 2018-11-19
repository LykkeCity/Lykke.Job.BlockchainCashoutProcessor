using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.CrossClient;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.CrossClient
{
    [UsedImplicitly]
    public class NotifyCrossClientCashoutCompletedCommandsHandler
    {
        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyCrossClientCashoutCompletedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent
            (
                new CrossClientCashoutCompletedEvent
                {
                    OperationId = command.OperationId,
                    ClientId = command.ClientId,
                    CashinOperationId = command.CashinOperationId,
                    RecipientClientId = command.RecipientClientId,
                    AssetId = command.AssetId,
                    Amount = command.Amount,
                    StartMoment = command.StartMoment,
                    FinishMoment = command.FinishMoment
                }
            );

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
