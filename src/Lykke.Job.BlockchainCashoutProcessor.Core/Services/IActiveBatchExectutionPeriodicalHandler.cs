using System;
using Autofac;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Services
{
    public interface IActiveBatchExectutionPeriodicalHandler :
        IStartable,
        IDisposable
    {
    }
}
