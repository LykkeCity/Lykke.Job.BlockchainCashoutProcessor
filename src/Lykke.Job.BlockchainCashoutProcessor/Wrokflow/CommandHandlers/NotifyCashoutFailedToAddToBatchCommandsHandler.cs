using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class NotifyCashoutFailedToAddToBatchCommandsHandler
    {
        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyCashoutFailedToAddToBatchCommand command, IEventPublisher publisher)
        {
            adsasd

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
