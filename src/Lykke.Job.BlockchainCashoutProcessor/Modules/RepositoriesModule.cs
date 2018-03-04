using Autofac;
using Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.AzureRepositories;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Core.Repositories;
using Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class RepositoriesModule : Module
    {
        private readonly IReloadingManager<DbSettings> _dbSettings;
        private readonly ILog _log;

        public RepositoriesModule(
            IReloadingManager<DbSettings> dbSettings,
            ILog log)
        {
            _log = log;
            _dbSettings = dbSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => CashoutRepository.Create(_dbSettings.Nested(x => x.DataConnString), _log))
                .As<ICashoutRepository>()
                .SingleInstance();

            builder.Register(c => CrossClientCashoutRepository.Create(_dbSettings.Nested(x => x.DataConnString), _log))
                .As<ICrossClientCashoutRepository>()
                .SingleInstance();

            builder.Register(c => MatchingEngineCallsDeduplicationRepository.Create(_dbSettings.Nested(x => x.DataConnString), _log))
                .As<IMatchingEngineCallsDeduplicationRepository>();
        }
    }
}
