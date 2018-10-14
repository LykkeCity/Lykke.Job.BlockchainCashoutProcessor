using System;
using JetBrains.Annotations;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain
{
    public class BlockchainConfiguration
    {
        public string HotWalletAddress { get; }
        public bool AreCashoutsDisabled { get; }

        [CanBeNull]
        public CashoutsAggregationConfiguration CashoutsAggregation { get; }

        public bool SupportCashoutAggregation => CashoutsAggregation != null;

        public BlockchainConfiguration(string hotWalletAddress, bool areCashoutsDisabled, [CanBeNull] CashoutsAggregationConfiguration cashoutsAggregation)
        {
            if (string.IsNullOrWhiteSpace(hotWalletAddress))
            {
                throw new ArgumentException("Should be not empty", nameof(hotWalletAddress));
            }

            HotWalletAddress = hotWalletAddress;
            AreCashoutsDisabled = areCashoutsDisabled;

            CashoutsAggregation = cashoutsAggregation;
        }
    }
}
