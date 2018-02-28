using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services;
using Lykke.Job.BlockchainCashoutProcessor.Services;
using Lykke.Job.BlockchainCashoutProcessor.Settings;
using Lykke.Job.BlockchainCashoutProcessor.Settings.Assets;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.OperationsRepository.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class JobModule : Module
    {
        private readonly AssetsSettings _assetsSettings;
        private readonly ChaosSettings _chaosSettings;
        private readonly Settings.OperationsRepositoryServiceClientSettings _operationsRepositoryServiceSettings;

        private readonly ILog _log;
        private readonly MatchingEngineSettings _meSettings;

        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public JobModule(
            AssetsSettings assetsSettings,
            ChaosSettings chaosSettings,
            Settings.OperationsRepositoryServiceClientSettings operationsRepositoryServiceSettings,
            MatchingEngineSettings matchingEngineSettings,
            ILog log)
        {
            _assetsSettings = assetsSettings;
            _chaosSettings = chaosSettings;
            _operationsRepositoryServiceSettings = operationsRepositoryServiceSettings;
            _log = log;
            _meSettings = matchingEngineSettings;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            _services.RegisterAssetsClient(new AssetServiceSettings
            {
                BaseUri = new Uri(_assetsSettings.ServiceUrl),
                AssetsCacheExpirationPeriod = _assetsSettings.CacheExpirationPeriod,
                AssetPairsCacheExpirationPeriod = _assetsSettings.CacheExpirationPeriod
            });

            builder.RegisterOperationsRepositoryClients(new Service.OperationsRepository.Client.OperationsRepositoryServiceClientSettings
            {
                ServiceUrl = _operationsRepositoryServiceSettings.ServiceUrl,
                RequestTimeout = _operationsRepositoryServiceSettings.RequestTimeout
            }, _log);

            RegisterMatchingEngine(builder);

            builder.RegisterChaosKitty(_chaosSettings);

            builder.Populate(_services);
        }

        private void RegisterMatchingEngine(ContainerBuilder builder)
        {
            var socketLog = new SocketLogDynamic(
                i => { },
                str => _log.WriteInfoAsync("ME client", "", str));

            builder.BindMeClient(_meSettings.IpEndpoint.GetClientIpEndPoint(), socketLog);
        }
    }
}
