using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services;
using Lykke.Job.BlockchainCashoutProcessor.Services;
using Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class BlockchainsModule : Module
    {
        private readonly BlockchainsIntegrationSettings _blockchainsIntegrationSettings;
        private readonly BlockchainWalletsServiceClientSettings _blockchainWalletsServiceClientSettings;

        public BlockchainsModule(
            BlockchainsIntegrationSettings blockchainsIntegrationSettings,
            BlockchainWalletsServiceClientSettings blockchainWalletsServiceClientSettings)
        {
            _blockchainsIntegrationSettings = blockchainsIntegrationSettings ?? throw new ArgumentNullException(nameof(blockchainsIntegrationSettings));
            _blockchainWalletsServiceClientSettings = blockchainWalletsServiceClientSettings ?? throw new ArgumentNullException(nameof(blockchainWalletsServiceClientSettings));
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BlockchainWalletsClient>()
                .As<IBlockchainWalletsClient>()
                .WithParameter(TypedParameter.From(_blockchainWalletsServiceClientSettings.ServiceUrl))
                .SingleInstance();

            var blockchainConfigurations = new Dictionary<string, BlockchainConfiguration>();

            foreach (var blockchain in _blockchainsIntegrationSettings.Blockchains.Where(b => !b.IsDisabled))
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
