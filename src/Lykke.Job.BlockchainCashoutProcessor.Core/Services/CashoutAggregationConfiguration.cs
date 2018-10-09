using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Services
{
    public class CashoutAggregationConfiguration
    {
        public TimeSpan AgeThreshold { get; }

        public int CountThreshold { get; }

        public CashoutAggregationConfiguration(TimeSpan ageThreshold, int countThreshold)
        {
            AgeThreshold = ageThreshold;
            CountThreshold = countThreshold;
        }
    }
}
