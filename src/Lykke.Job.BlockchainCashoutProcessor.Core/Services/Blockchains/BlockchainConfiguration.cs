﻿using System;
using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains
{
    public class BlockchainConfiguration
    {
        public string HotWalletAddress { get; }
        public bool AreCashoutsDisabled { get; }

        [CanBeNull]
        public BlockchainCashoutAggregationConfiguration CashoutAggregation { get; }

        public bool SupportCashoutAggregation => CashoutAggregation != null;

        public BlockchainConfiguration(string hotWalletAddress, bool areCashoutsDisabled, [CanBeNull] BlockchainCashoutAggregationConfiguration cashoutAggregation)
        {
            if (string.IsNullOrWhiteSpace(hotWalletAddress))
            {
                throw new ArgumentException("Should be not empty", nameof(hotWalletAddress));
            }

            HotWalletAddress = hotWalletAddress;
            AreCashoutsDisabled = areCashoutsDisabled;

            CashoutAggregation = cashoutAggregation;
        }
    }
}
