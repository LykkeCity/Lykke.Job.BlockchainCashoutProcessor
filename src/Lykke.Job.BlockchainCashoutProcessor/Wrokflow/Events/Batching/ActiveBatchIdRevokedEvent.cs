using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class ActiveBatchIdRevokedEvent
    {
        public Guid BatchId { get; set; }
    }
}
