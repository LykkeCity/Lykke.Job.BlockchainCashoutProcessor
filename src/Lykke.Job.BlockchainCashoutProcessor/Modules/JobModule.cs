using System;
using Autofac;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashoutProcessor.AppServices.Health;
using Lykke.Job.BlockchainCashoutProcessor.AppServices.Lifecycle;
using Lykke.Job.BlockchainCashoutProcessor.Settings;
using Lykke.Job.BlockchainCashoutProcessor.Settings.Assets;
using Lykke.Service.Assets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class JobModule : Module
    {
        private readonly AssetsSettings _assetsSettings;
        private readonly ChaosSettings _chaosSettings;

        private readonly MatchingEngineSettings _meSettings;

        public JobModule(
            AssetsSettings assetsSettings,
            ChaosSettings chaosSettings,
            MatchingEngineSettings matchingEngineSettings)
        {
            _assetsSettings = assetsSettings;
            _chaosSettings = chaosSettings;
            _meSettings = matchingEngineSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            builder.RegisterAssetsClient
            (
                new AssetServiceSettings
                {
                    BaseUri = new Uri(_assetsSettings.ServiceUrl),
                    AssetsCacheExpirationPeriod = _assetsSettings.CacheExpirationPeriod,
                    AssetPairsCacheExpirationPeriod = _assetsSettings.CacheExpirationPeriod
                }
            );

            RegisterMatchingEngine(builder);

            builder.RegisterChaosKitty(_chaosSettings);
        }

        private void RegisterMatchingEngine(ContainerBuilder builder)
        {
            builder.RegisgterMeClient(_meSettings.IpEndpoint.GetClientIpEndPoint());
        }
    }
}
