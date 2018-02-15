using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    [Obsolete("Should be removed with next release")]
    [MessagePackObject]
    public class ClientOperationFinishRegisteredEvent
    {
        [Key(0)]
        public Guid OperationId { get; set; }
    }
}
