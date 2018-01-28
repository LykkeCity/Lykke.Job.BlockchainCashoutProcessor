using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.SlackNotifications
{
    [UsedImplicitly]
    public class SlackNotificationsSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public AzureQueuePublicationSettings AzureQueue { get; set; }
    }
}
