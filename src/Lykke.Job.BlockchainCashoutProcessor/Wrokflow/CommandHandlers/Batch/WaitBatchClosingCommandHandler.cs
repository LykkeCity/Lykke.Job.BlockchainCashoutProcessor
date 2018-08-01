using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batch;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batch
{
    public class WaitBatchClosingCommandHandler
    {
        private readonly RetryDelayProvider _retryDelayProvider;

        public WaitBatchClosingCommandHandler(RetryDelayProvider retryDelayProvider)
        {
            _retryDelayProvider = retryDelayProvider;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(WaitBatchClosingCommand command,
            IEventPublisher publisher)
        {
            throw new NotImplementedException();
        }
    }
}
