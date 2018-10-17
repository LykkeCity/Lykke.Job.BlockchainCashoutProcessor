using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CompleteBatchCommand
    {
        public Guid BatchId { get; set; }
        public decimal TransactionFee { get; set; }
        public string TransactionHash { get; set; }
    }
}
