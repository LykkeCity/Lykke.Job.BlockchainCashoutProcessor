using System;
using ProtoBuf;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    [ProtoContract]
    public class CashoutStartedEvent
    {
        [ProtoMember(1)]
        public Guid OperationId { get; set; }

        /// <summary>
        /// Lykke asset ID.
        /// </summary>
        [ProtoMember(2)]
        public string AssetId { get; set; }

        [ProtoMember(3)]
        public string HotWalletAddress { get; set; }

        [ProtoMember(4)]
        public string ToAddress { get; set; }

        [ProtoMember(5)]
        public decimal Amount { get; set; }
    }
}
