using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CashoutAddedToBatchEvent
    {
        public Guid BatchId { get; set; }
        public int CashoutsCount { get; set; }
        public int CashoutsCountThreshold { get; set; }
    }
}
