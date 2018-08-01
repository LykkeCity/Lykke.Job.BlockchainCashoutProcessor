using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batch
{
    [MessagePackObject]
    public class CashoutBatchOperationAddedEvent
    {
        [Key(0)]
        public Guid BatchId { get; set; }

        [Key(1)]
        public Guid OperationId { get; set; }

        [Key(2)]
        public decimal OperationAmount { get; set; }
        
        [Key(3)]
        public string OperationDestinationAddress { get; set; }
    }
}
