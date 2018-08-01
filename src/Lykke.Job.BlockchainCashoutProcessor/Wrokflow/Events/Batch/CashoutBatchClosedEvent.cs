using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batch
{
    [MessagePackObject]
    public class CashoutBatchClosedEvent
    {
        [Key(0)]
        public Guid BatchId { get; set; }
    }
}
