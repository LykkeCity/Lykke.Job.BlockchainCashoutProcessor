using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    [MessagePackObject]
    public class BatchedCashout
    {
        [Key(0)]
        public Guid OperationId { get; set; }
        
        [Key(1)]
        public string DestinationAddress { get; set; }
        
        [Key(2)]
        public decimal Amount { get; set; }
    }
}
