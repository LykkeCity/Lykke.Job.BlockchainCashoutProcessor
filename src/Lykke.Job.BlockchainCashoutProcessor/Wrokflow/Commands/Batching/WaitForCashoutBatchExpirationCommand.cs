using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class WaitForCashoutBatchExpirationCommand
    {
        public Guid BatchId { get;set; }
    }
}
