using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class NotifyCashinCompletedCommand
    {
        public Guid ClientId { get; set; }
        public decimal Amount { get; set; }
        public string AssetId { get; set; }
        public Guid OperationId { get; set; }
        public string TransactionHash { get; set; }
    }
}
