using System;
using System.Collections.Generic;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batch
{
    [MessagePackObject]
    public class BatchClosedEvent
    {
        [Key(0)]
        public Guid BatchId { get; set; }

        [Key(0)]
        public IEnumerable<(Guid operationId, decimal amount, string destinationAddress)> Operations { get; set; }
    }
}
