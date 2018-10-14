using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.AppServices.Lifecycle
{
    // NOTE: Sometimes, shutdown process should be expressed explicitly. 
    // If this is your case, use this class to manage shutdown.
    // For example, sometimes some state should be saved only after all incoming message processing and 
    // all periodical handler was stopped, and so on.
    [UsedImplicitly]
    public class ShutdownManager : IShutdownManager
    {
        public async Task StopAsync()
        {
            // TODO: Implement your shutdown logic here. Good idea is to log every step

            await Task.CompletedTask;
        }
    }
}
