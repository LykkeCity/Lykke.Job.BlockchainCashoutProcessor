using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    [MessagePackObject]
    public class CrossClientCashoutStartedEvent
    {
        [Key(0)]
        public Guid OperationId { get; set; }

        [Key(1)]
        public string BlockchainType { get; set; }

        [Key(2)]
        public string BlockchainAssetId { get; set; }

        /// <summary>
        /// Lykke asset ID.
        /// </summary>
        [Key(3)]
        public string AssetId { get; set; }

        [Key(4)]
        public string ToAddress { get; set; }

        [Key(5)]
        public decimal Amount { get; set; }

        [Key(6)]
        public Guid FromClientId { get; set; }

        [Key(7)]
        public Guid ToClientId { get; set; }

        [Key(8)]
        public string HotWalletAddress { get; set; }
    }
}
