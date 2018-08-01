using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains
{
    public class BlockchainCashoutAggregationConfiguration
    {
        public TimeSpan MaxPeriod { get; }

        public int MaxCount { get; }

        public BlockchainCashoutAggregationConfiguration(TimeSpan maxPeriod, int maxCount)
        {
            MaxPeriod = maxPeriod;
            MaxCount = maxCount;
        }
    }
}
