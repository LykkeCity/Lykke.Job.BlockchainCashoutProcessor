using System;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class NotifyCashinCompletedCommand
    {
        public Guid ClientId { get; set; }
        public decimal Amount { get; set; }
        public decimal TransactionAmount { get; set; }
        public decimal TransactionFee { get; set; }
        public string AssetId { get; set; }
        public Guid OperationId { get; set; }
        public string TransactionHash { get; set; }
    }
}
