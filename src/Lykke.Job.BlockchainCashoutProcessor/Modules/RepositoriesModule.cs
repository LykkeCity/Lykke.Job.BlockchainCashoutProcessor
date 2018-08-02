using Autofac;
using Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.AzureRepositories;
using Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batch;
using Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.ActiveBatch;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batch;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.CrossClient;
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

            builder.Register(c => CashoutBatchRepository.Create(_dbSettings.Nested(x => x.DataConnString), _log))
                .As<ICashoutBatchRepository>()
                .SingleInstance();

            builder.Register(c => CrossClientCashoutRepository.Create(_dbSettings.Nested(x => x.DataConnString), _log))
                .As<ICrossClientCashoutRepository>()
                .SingleInstance();

            builder.Register(c => ActiveBatchRepository.Create(_dbSettings.Nested(x => x.DataConnString), _log))
                .As<IActiveBatchRepository>()
                .SingleInstance();

            builder.Register(c => MatchingEngineCallsDeduplicationRepository.Create(_dbSettings.Nested(x => x.DataConnString), _log))
                .As<IMatchingEngineCallsDeduplicationRepository>()
                .SingleInstance();
        }
    }
}
