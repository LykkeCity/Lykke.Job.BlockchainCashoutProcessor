using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.RiskControl
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class AcceptCashoutCommand
    {
        public Guid OperationId { get; internal set; }
        public Guid ClientId { get; internal set; }
        public string AssetId { get; internal set; }
        public string BlockchainType { get; internal set; }
        public string BlockchainAssetId { get; internal set; }
        public string HotWalletAddress { get; internal set; }
        public string ToAddress { get; internal set; }
        public decimal Amount { get; internal set; }
    }
}