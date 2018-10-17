﻿using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.AppServices.Lifecycle
{
    // NOTE: Sometimes, startup process which is expressed explicitly is not just better, 
    // but the only way. If this is your case, use this class to manage startup.
    // For example, sometimes some state should be restored before any periodical handler will be started, 
    // or any incoming message will be processed and so on.
    // Do not forget to remove As<IStartable>() and AutoActivate() from DI registartions of services, 
    // which you want to startup explicitly.
    [UsedImplicitly]
    public class StartupManager : IStartupManager
    {
        public async Task StartAsync()
        {
            // TODO: Implement your startup logic here. Good idea is to log every step

            await Task.CompletedTask;
        }
    }
}
