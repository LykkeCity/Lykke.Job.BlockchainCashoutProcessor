using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Services
{
    public interface IStartupManager
    {
        Task StartAsync();
    }
}