using JetBrains.Annotations;
using Lykke.Job.BlockchainCashoutProcessor.Settings.Assets;
using Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain;
using Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings;
using Lykke.Job.BlockchainCashoutProcessor.Settings.SlackNotifications;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings
{
    [UsedImplicitly]
    public class AppSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BlockchainCashoutProcessorSettings BlockchainCashoutProcessorJob { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BlockchainsIntegrationSettings BlockchainsIntegration { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public SlackNotificationsSettings SlackNotifications { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public AssetsSettings Assets { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public OperationsRepositoryServiceClientSettings OperationsRepositoryServiceClient { get; set; }
    }
}
