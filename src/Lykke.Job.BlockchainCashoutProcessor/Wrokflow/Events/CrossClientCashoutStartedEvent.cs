using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CrossClientCashoutStartedEvent
    {
        public Guid OperationId { get; set; }
        public string BlockchainType { get; set; }
        public string BlockchainAssetId { get; set; }
        public string AssetId { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public Guid FromClientId { get; set; }
        public Guid RecipientClientId { get; set; }
        public string HotWalletAddress { get; set; }
    }
}
