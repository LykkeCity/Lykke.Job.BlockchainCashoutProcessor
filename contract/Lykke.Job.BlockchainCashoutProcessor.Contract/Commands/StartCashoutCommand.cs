using System;
using JetBrains.Annotations;
using MessagePack;
using ProtoBuf;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Commands
{
    /// <summary>
    /// Command to start the cashout in the generic blockchain integration layer
    /// </summary>
    [PublicAPI]
    [MessagePackObject]
    [ProtoContract]
    public class StartCashoutCommand
    {
        /// <summary>
        /// Cashout operation ID.
        /// </summary>
        [Key(0)]
        [ProtoMember(0)]
        public Guid OperationId { get; set; }

        /// <summary>
        /// Lykke asset ID.
        /// </summary>
        [Key(1)]
        [ProtoMember(1)]
        public string AssetId { get; set; }

        /// <summary>
        /// Recipient address.
        /// </summary>
        [Key(2)]
        [ProtoMember(2)]
        public string ToAddress { get; set; }

        /// <summary>
        /// Amount of the funds to cashout.
        /// Should be positive number.
        /// </summary>
        [Key(3)]
        [ProtoMember(3)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Lykke client ID.
        /// </summary>
        [Key(4)]
        [ProtoMember(4)]
        public Guid ClientId { get; set; }
    }
}
