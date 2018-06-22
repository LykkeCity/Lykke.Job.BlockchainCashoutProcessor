namespace Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains
{
    public class BlockchainConfiguration
    {
        public string HotWalletAddress { get; }
        public bool AreCashoutsDisabled { get; }

        public BlockchainConfiguration(string hotWalletAddress, bool areCashoutsDisabled)
        {
            HotWalletAddress = hotWalletAddress;
            AreCashoutsDisabled = areCashoutsDisabled;
        }
    }
}
