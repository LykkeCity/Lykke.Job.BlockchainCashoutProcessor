using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    [MessagePackObject]
    public class NotifyCashoutFailedToAddToBatchCommand
    {
        [Key(0)]
        public Guid OperationId { get; set; }
    }
}
