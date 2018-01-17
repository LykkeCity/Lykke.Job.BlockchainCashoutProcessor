namespace Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains
{
    public interface IHotWalletsProvider
    {
        string GetHotWalletAddress(string blockchainType);
    }
}
