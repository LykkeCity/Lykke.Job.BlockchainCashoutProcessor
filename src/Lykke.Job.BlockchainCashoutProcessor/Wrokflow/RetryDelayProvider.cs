using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow
{
    public class RetryDelayProvider
    {
        public TimeSpan WaitForBatchClosingRetryDelay { get; }

        public RetryDelayProvider(TimeSpan waitForBatchClosingRetryDelay)
        {
            WaitForBatchClosingRetryDelay = waitForBatchClosingRetryDelay;
        }

    }
}
