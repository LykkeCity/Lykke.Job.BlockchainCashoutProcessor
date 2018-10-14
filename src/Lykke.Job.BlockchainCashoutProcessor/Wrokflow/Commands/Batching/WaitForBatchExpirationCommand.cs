using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class WaitForBatchExpirationCommand
    {
        public Guid BatchId { get;set; }
    }
}
