using System;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CloseBatchCommand
    {
        public Guid BatchId { get; set; }
        public CashoutsBatchClosingReason Reason { get; set; }
    }
}
