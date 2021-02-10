using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Abstractions.Middleware;
using Lykke.Cqrs.Middleware;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Interceptors
{
    public class ErrorsCommandInterceptor : ICommandInterceptor
    {
        private readonly ILog _log;

        public ErrorsCommandInterceptor(ILogFactory logFactory)
        {
            _log = logFactory.CreateLog(this);
        }

        public async Task<CommandHandlingResult> InterceptAsync(ICommandInterceptionContext context)
        {
            try
            {
                var result = await context.InvokeNextAsync();
                return result;
            }
            catch (InvalidAggregateStateException ex)
            {
                _log.Warning($"{nameof(InvalidAggregateStateException)} handled", ex);
                return CommandHandlingResult.Fail(TimeSpan.FromSeconds(10));
            }
        }
    }
}
