using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batch;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batch
{
    public class CloseBatchCommandHandler
    {
        private readonly IChaosKitty _chaosKitty;

        public CloseBatchCommandHandler(IChaosKitty chaosKitty)
        {
            _chaosKitty = chaosKitty;
        }


        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(CloseBatchCommand command, IEventPublisher publisher)
        {
            throw new NotImplementedException();
        }
    }
}
