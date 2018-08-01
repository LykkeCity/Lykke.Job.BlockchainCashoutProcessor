using Lykke.Service.BlockchainApi.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains
{
    public interface IBlockchainApiClientProvider
    {
        IBlockchainApiClient Get(string blockchainType);
    }
}
