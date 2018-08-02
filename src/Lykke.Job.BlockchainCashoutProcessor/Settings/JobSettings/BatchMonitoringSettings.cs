using System;
using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings
{
    [UsedImplicitly]
    public class BatchMonitoringSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public TimeSpan Period { get; set; }
    }
}
