using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class NotifyCashoutCompletedCommand
    {
        public string AssetId { get; set; }

        public string ToAddress { get; set; }

        public decimal Amount { get; set; }

        public Guid ClientId { get; set; }

        public Guid OperationId { get; set; }

        public string TransactionHash { get; set; }
    }
}
