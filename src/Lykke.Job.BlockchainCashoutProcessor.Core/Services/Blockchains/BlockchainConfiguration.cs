using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains
{
    public class BlockchainConfiguration
    {
        public string HotWalletAddress { get; }
        public bool AreCashoutsDisabled { get; }

        public BlockchainConfiguration(string hotWalletAddress, bool areCashoutsDisabled)
        {
            if (string.IsNullOrWhiteSpace(hotWalletAddress))
            {
                throw new ArgumentException("Should be not empty", nameof(hotWalletAddress));
            }

            HotWalletAddress = hotWalletAddress;
            AreCashoutsDisabled = areCashoutsDisabled;
        }
    }
}
