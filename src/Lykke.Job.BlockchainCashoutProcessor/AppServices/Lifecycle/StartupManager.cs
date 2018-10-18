using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;

namespace Lykke.Job.BlockchainCashoutProcessor.AppServices.Lifecycle
{
    [UsedImplicitly]
    public class StartupManager : IStartupManager
    {
        private readonly ICqrsEngine _cqrsEngine;

        public StartupManager(ICqrsEngine cqrsEngine)
        {
            _cqrsEngine = cqrsEngine;
        }

        public async Task StartAsync()
        {
            _cqrsEngine.Start();

            await Task.CompletedTask;
        }
    }
}
