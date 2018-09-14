using System;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class NotifyCashinCompletedCommand
    {
        public Guid ClientId { get; set; }
        public decimal Amount { get; set; }
        public decimal MeOperationAmount { get; set; }
        public decimal Fee { get; set; }
        public string AssetId { get; set; }
        public CashinOperationType OperationType { get; set; }
        public Guid OperationId { get; set; }
        public string TransactionHash { get; set; }
    }
}
