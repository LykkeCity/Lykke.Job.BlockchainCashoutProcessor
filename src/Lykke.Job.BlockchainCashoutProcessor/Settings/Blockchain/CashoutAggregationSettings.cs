using System;
using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain
{
    [UsedImplicitly]
    public class CashoutAggregationSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public TimeSpan AgeThreshold { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public int CountThreshold { get; set; }
    }
}
