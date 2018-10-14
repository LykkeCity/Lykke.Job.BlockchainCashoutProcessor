using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Regular
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class NotifyCashoutCompletedCommand
    {
        public string AssetId { get; set; }

        public string ToAddress { get; set; }

        public decimal Amount { get; set; }

        public decimal TransactionAmount { get; set; }

        public decimal TransactionFee { get; set; }

        public Guid ClientId { get; set; }

        public Guid OperationId { get; set; }

        public string TransactionHash { get; set; }

        public DateTime StartMoment { get; set; }

        public DateTime FinishMoment { get; set; }
    }
}
