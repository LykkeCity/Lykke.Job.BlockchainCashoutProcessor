﻿using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.AppServices.Lifecycle
{
    public interface IStartupManager
    {
        Task StartAsync();
    }
}
