using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    [MessagePackObject]
    public class BatchSuspendedEvent
    {
        [Key(0)]
        public Guid BatchId { get; set; }

        [Key(1)]
        public BatchedCashout[] Cashouts { get; set; }
    }
}
