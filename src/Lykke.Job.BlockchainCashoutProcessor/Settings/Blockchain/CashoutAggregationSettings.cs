using System;
using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain
{
    [UsedImplicitly]
    public class CashoutAggregationSettings
    {
        public TimeSpan AgeThreshold { get; set; }

        public int CountThreshold { get; set; }
    }
}
