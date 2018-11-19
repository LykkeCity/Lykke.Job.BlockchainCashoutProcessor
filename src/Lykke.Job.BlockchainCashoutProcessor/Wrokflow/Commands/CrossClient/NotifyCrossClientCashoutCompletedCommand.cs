using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.CrossClient
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class NotifyCrossClientCashoutCompletedCommand
    {
        public Guid OperationId { get; set; }
        public Guid CashinOperationId { get; set; }
        public Guid ClientId { get; set; }
        public Guid RecipientClientId { get; set; }
        public decimal Amount { get; set; }
        public string AssetId { get; set; }
        public DateTime StartMoment { get; set; }
        public DateTime FinishMoment { get; set; }
    }
}
