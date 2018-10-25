using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class BatchFillingStartedEvent
    {
        public Guid BatchId { get; set; }
    }
}
