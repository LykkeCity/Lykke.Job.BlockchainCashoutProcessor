using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Services
{
    public interface IBlockchainConfigurationsProvider
    {
        BlockchainConfiguration GetConfiguration(string blockchainType);
    }
}
