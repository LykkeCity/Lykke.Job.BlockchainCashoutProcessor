using System;
using System.Collections.Generic;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    [MessagePackObject]
    public class BatchSuspendedEvent
    {
        [Key(0)]
        public Guid BatchId { get; set; }

        [Key(0)]
        public IEnumerable<(Guid operationId, decimal amount, string destinationAddress)> Operations { get; set; }
    }
}
