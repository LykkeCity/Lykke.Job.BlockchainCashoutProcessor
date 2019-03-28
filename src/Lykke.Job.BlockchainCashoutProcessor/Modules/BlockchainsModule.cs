using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Lykke.HttpClientGenerator.Caching;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services;
using Lykke.Job.BlockchainCashoutProcessor.Services;
using Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain;
using Lykke.Service.BlockchainSettings.Client;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class BlockchainsModule : Module
    {
        private readonly BlockchainWalletsServiceClientSettings _blockchainWalletsServiceClientSettings;
        private readonly BlockchainSettingsServiceClientSettings _blockchainSettingsServiceClient;

        public BlockchainsModule(
            BlockchainSettingsServiceClientSettings blockchainSettingsServiceClient,
            BlockchainWalletsServiceClientSettings blockchainWalletsServiceClientSettings)
        {
            _blockchainSettingsServiceClient = blockchainSettingsServiceClient?? throw new ArgumentNullException(nameof(blockchainSettingsServiceClient));
            _blockchainWalletsServiceClientSettings = blockchainWalletsServiceClientSettings ?? throw new ArgumentNullException(nameof(blockchainWalletsServiceClientSettings));
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BlockchainWalletsClient>()
                .As<IBlockchainWalletsClient>()
                .WithParameter(TypedParameter.From(_blockchainWalletsServiceClientSettings.ServiceUrl))
                .SingleInstance();

            var cacheManager = new ClientCacheManager();
            var factory =
                new Lykke.Service.BlockchainSettings.Client.HttpClientGenerator.BlockchainSettingsClientFactory();
            var client = factory.CreateNew(
                _blockchainSettingsServiceClient.ServiceUrl,
                _blockchainSettingsServiceClient.ApiKey,
                true,
                cacheManager);
            builder.RegisterInstance(client)
                .As<IBlockchainSettingsClient>();
            builder.RegisterInstance(cacheManager)
                .As<IClientCacheManager>()
                .SingleInstance();

            var allSettings = client.GetAllSettingsAsync().Result;

            if (allSettings?.Collection == null || !allSettings.Collection.Any())
            {
                throw new Exception("There is no/or empty response from Lykke.Service.BlockchainSettings. " +
                                    "It is impossible to start CashoutProcessor");
            }
            var blockchainIntegrations = allSettings.Collection.ToList();
            var blockchainConfigurations = new Dictionary<string, BlockchainConfiguration>();

            foreach (var blockchain in blockchainIntegrations)
            {
                var blockchainConfiguration = new BlockchainConfiguration
                (
                    blockchain.HotWalletAddress,
                    blockchain.AreCashoutsDisabled,
                    blockchain.CashoutAggregation != null ? 
                        new CashoutsAggregationConfiguration(
                            blockchain.CashoutAggregation.AgeThreshold, 
                            blockchain.CashoutAggregation.CountThreshold):
                        null
                );

                blockchainConfigurations.Add(blockchain.Type, blockchainConfiguration);
            }

            builder.RegisterType<BlockchainConfigurationsProvider>()
                .As<IBlockchainConfigurationsProvider>()
                .WithParameter(TypedParameter.From<IReadOnlyDictionary<string, BlockchainConfiguration>>(blockchainConfigurations))
                .SingleInstance();
        }
    }
}
