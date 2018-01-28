using System.Collections.Generic;
using System.Linq;
using Autofac;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;
using Lykke.Job.BlockchainCashoutProcessor.Services.Blockchains;
using Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain;

namespace Lykke.Job.BlockchainCashoutProcessor.Modules
{
    public class BlockchainsModule : Module
    {
        private readonly BlockchainsIntegrationSettings _blockchainsIntegrationSettings;

        public BlockchainsModule(BlockchainsIntegrationSettings blockchainsIntegrationSettings)
        {
            _blockchainsIntegrationSettings = blockchainsIntegrationSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HotWalletsProvider>()
                .As<IHotWalletsProvider>()
                .WithParameter(TypedParameter.From<IReadOnlyDictionary<string, string>>(_blockchainsIntegrationSettings.Blockchains.ToDictionary(b => b.Type, b => b.HotWalletAddress)))
                .SingleInstance();
        }
    }
}
