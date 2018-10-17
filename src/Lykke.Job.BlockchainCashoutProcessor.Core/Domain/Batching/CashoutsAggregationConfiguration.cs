using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public class CashoutsAggregationConfiguration
    {
        public TimeSpan AgeThreshold { get; }

        public int CountThreshold { get; }

        public CashoutsAggregationConfiguration(TimeSpan ageThreshold, int countThreshold)
        {
            AgeThreshold = ageThreshold;
            CountThreshold = countThreshold;
        }
    }
}
