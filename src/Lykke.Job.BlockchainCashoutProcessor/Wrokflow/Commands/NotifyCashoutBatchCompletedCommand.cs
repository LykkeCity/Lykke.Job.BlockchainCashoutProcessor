using System;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class NotifyCashoutBatchCompletedCommand
    {
        public Guid BatchId { get; set; }

        public string BlockchainAssetId { get; set; }

        public string TransactionHash { get; set; }

        public BatchedCashout[] Cashouts { get; set; }
    }
}
