namespace Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains
{
    public interface IBlockchainConfigurationsProvider
    {
        BlockchainConfiguration GetConfiguration(string blockchainType);
    }
}
