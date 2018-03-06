using System.Collections.Generic;
using System.Linq;
using Autofac;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;
using Lykke.Job.BlockchainCashoutProcessor.Services.Blockchains;
using Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class BlockchainsModule : Module
    {
        private readonly BlockchainsIntegrationSettings _blockchainsIntegrationSettings;
        private readonly BlockchainWalletsServiceClientSettings _blockchainWalletsServiceClientSettings;

        public BlockchainsModule(BlockchainsIntegrationSettings blockchainsIntegrationSettings,
            BlockchainWalletsServiceClientSettings blockchainWalletsServiceClientSettings)
        {
            _blockchainsIntegrationSettings = blockchainsIntegrationSettings;
            _blockchainWalletsServiceClientSettings = blockchainWalletsServiceClientSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BlockchainWalletsClient>()
                .As<IBlockchainWalletsClient>()
                .WithParameter(TypedParameter.From(_blockchainWalletsServiceClientSettings.ServiceUrl))
                .SingleInstance();

            builder.RegisterType<HotWalletsProvider>()
                .As<IHotWalletsProvider>()
                .WithParameter(TypedParameter.From<IReadOnlyDictionary<string, string>>(_blockchainsIntegrationSettings.Blockchains.ToDictionary(b => b.Type, b => b.HotWalletAddress)))
                .SingleInstance();
        }
    }
}
