using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class AddToBatchCommandHandler
    {
        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(AddOperationToBatchCommand command, IEventPublisher publisher)
        {
            throw new NotImplementedException();
        }
    }
}
