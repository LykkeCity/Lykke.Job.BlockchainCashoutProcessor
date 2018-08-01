using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;
using Lykke.Job.BlockchainCashoutProcessor.Services.Blockchains;
using Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class BlockchainsModule : Module
    {
        private readonly ILog _log;
        private readonly BlockchainsIntegrationSettings _blockchainsIntegrationSettings;
        private readonly BlockchainWalletsServiceClientSettings _blockchainWalletsServiceClientSettings;

        public BlockchainsModule(
            ILog log,
            BlockchainsIntegrationSettings blockchainsIntegrationSettings,
            BlockchainWalletsServiceClientSettings blockchainWalletsServiceClientSettings)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
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

            builder.RegisterType<BlockchainApiClientProvider>()
                .As<IBlockchainApiClientProvider>();


            foreach (var blockchain in _blockchainsIntegrationSettings.Blockchains.Where(b => !b.IsDisabled))
            {
                _log.WriteInfo("Blockchains registration", "", 
                    $"Registering blockchain: {blockchain.Type} -> \r\nHW: {blockchain.HotWalletAddress}");

                if (blockchain.AreCashoutsDisabled)
                {
                    _log.WriteWarning("Blockchains registration", "", $"Cashouts for blockchain {blockchain.Type} are disabled");
                }

                var blockchainConfiguration = new BlockchainConfiguration
                (
                    blockchain.HotWalletAddress,
                    blockchain.AreCashoutsDisabled,
                    blockchain.CashoutAggregation != null ? 
                        new BlockchainCashoutAggregationConfiguration(blockchain.CashoutAggregation.MaxPeriod, 
                            blockchain.CashoutAggregation.MaxCount):
                        null
                );

                blockchainConfigurations.Add(blockchain.Type, blockchainConfiguration);

                builder.RegisterType<BlockchainApiClient>()
                    .Named<IBlockchainApiClient>(blockchain.Type)
                    .WithParameter(TypedParameter.From(blockchain.ApiUrl))
                    .SingleInstance();
            }

            builder.RegisterType<BlockchainConfigurationsProvider>()
                .As<IBlockchainConfigurationsProvider>()
                .WithParameter(TypedParameter.From<IReadOnlyDictionary<string, BlockchainConfiguration>>(blockchainConfigurations))
                .SingleInstance();
        }
    }
}
