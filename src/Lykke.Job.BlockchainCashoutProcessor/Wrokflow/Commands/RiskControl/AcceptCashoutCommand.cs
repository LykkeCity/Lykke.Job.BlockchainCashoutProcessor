using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.RiskControl
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class AcceptCashoutCommand
    {
        public Guid OperationId { get; set; }
        public Guid ClientId { get; set; }
        public string AssetId { get; set; }
        public string BlockchainType { get; set; }
        public string BlockchainAssetId { get; set; }
        public string HotWalletAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
    }
}