using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;

namespace Lykke.Job.BlockchainCashoutProcessor.AppServices.Lifecycle
{
    [UsedImplicitly]
    public class StartupManager : IStartupManager
    {
        private readonly ICqrsEngine _cqrsEngine;
        private ILog _log;

        public StartupManager(
            ILogFactory logFactory,
            ICqrsEngine cqrsEngine)
        {
            _log = logFactory.CreateLog(this);
            _cqrsEngine = cqrsEngine;
        }

        public async Task StartAsync()
        {
            _log.Info("Starting cqrs engine...");

            _cqrsEngine.Start();

            await Task.CompletedTask;
        }
    }
}
