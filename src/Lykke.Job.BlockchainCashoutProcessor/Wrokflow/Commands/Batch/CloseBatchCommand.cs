using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batch
{
    [MessagePackObject]
    public class CloseBatchCommand
    {
        [Key(0)]
        public Guid BatchId { get; set; }
    }
}
