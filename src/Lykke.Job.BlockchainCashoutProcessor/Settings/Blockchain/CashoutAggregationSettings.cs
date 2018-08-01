using System;
using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain
{
    [UsedImplicitly]
    public class CashoutAggregationSettings
    {
        public TimeSpan MaxPeriod { get; set; }

        public int MaxCount { get; set; }
    }
}
