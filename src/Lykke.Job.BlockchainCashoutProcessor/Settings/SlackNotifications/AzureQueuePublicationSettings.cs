﻿using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.SlackNotifications
{
    [UsedImplicitly]
    public class AzureQueuePublicationSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string ConnectionString { get; set; }
        
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string QueueName { get; set; }
    }
}
