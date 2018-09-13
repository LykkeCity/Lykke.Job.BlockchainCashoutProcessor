using System;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class NotifyCashoutFailedCommand
    {
        public Guid ClientId { get; set; }
        public decimal Amount { get; set; }
        public string AssetId { get; set; }
        public Guid OperationId { get; set; }
        public string Error { get; set; }
        public CashoutErrorCode ErrorCode { get; set; }
    }
}
