using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    [MessagePackObject]
    public class NotifyCashoutSuccessfullyAddedToBatchCommand
    {
        [Key(0)]
        public Guid OperationId { get; set; }

        [Key(1)]
        public Guid BatchId { get; set; }
    }
}
