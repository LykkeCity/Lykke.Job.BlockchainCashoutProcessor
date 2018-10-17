using Autofac;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching;
using Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Regular;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Regular;
using Lykke.Job.BlockchainCashoutProcessor.Core.Repositories;
using Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class RepositoriesModule : Module
    {
        private readonly IReloadingManager<DbSettings> _dbSettings;

        public RepositoriesModule(IReloadingManager<DbSettings> dbSettings)
        {
            _dbSettings = dbSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => CashoutRepository.Create(_dbSettings.Nested(x => x.DataConnString), c.Resolve<ILogFactory>()))
                .As<ICashoutRepository>()
                .SingleInstance();

            builder.Register(c => CashoutsBatchRepository.Create(_dbSettings.Nested(x => x.DataConnString), c.Resolve<ILogFactory>()))
                .As<ICashoutsBatchRepository>()
                .As<ICashoutsBatchReadOnlyRepository>()
                .SingleInstance();

            builder.Register(c => CrossClientCashoutRepository.Create(_dbSettings.Nested(x => x.DataConnString), c.Resolve<ILogFactory>()))
                .As<ICrossClientCashoutRepository>()
                .SingleInstance();

            builder.Register(c => ActiveCashoutsBatchIdRepository.Create(_dbSettings.Nested(x => x.DataConnString), c.Resolve<ILogFactory>()))
                .As<IActiveCashoutsBatchIdRepository>()
                .SingleInstance();

            builder.Register(c => MatchingEngineCallsDeduplicationRepository.Create(_dbSettings.Nested(x => x.DataConnString), c.Resolve<ILogFactory>()))
                .As<IMatchingEngineCallsDeduplicationRepository>()
                .SingleInstance();
        }
    }
}
