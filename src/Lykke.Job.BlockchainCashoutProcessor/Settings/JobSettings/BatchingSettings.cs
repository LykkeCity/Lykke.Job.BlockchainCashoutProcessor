using System;
using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings
{
    [UsedImplicitly]
    public class BatchingSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public TimeSpan ExpirationMonitoringPeriod { get; set; }
    }
}
