using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow
{
    public class RetryDelayProvider
    {
        public TimeSpan WaitForBatchClosingRetryDelay { get; }
        public TimeSpan DefaultRetryDelay { get; }

        public RetryDelayProvider(TimeSpan waitForBatchClosingRetryDelay, TimeSpan defaultRetryDelay)
        {
            WaitForBatchClosingRetryDelay = waitForBatchClosingRetryDelay;
            DefaultRetryDelay = defaultRetryDelay;
        }

    }
}
