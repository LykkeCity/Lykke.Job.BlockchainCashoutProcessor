using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class BatchIdRevokedEvent
    {
        public Guid BatchId { get; set; }
    }
}
