using System;
using JetBrains.Annotations;
using ProtoBuf;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Commands
{
    /// <summary>
    /// Command to start the cashout in the generic blockchain integration layer
    /// </summary>
    [PublicAPI]
    [ProtoContract]
    public class StartCashoutCommand
    {
        /// <summary>
        /// Cashout operation ID.
        /// </summary>
        [ProtoMember(1)]
        public Guid OperationId { get; set; }

        /// <summary>
        /// Lykke asset ID.
        /// </summary>
        [ProtoMember(2)]
        public string AssetId { get; set; }

        /// <summary>
        /// Recipient address.
        /// </summary>
        [ProtoMember(3)]
        public string ToAddress { get; set; }

        /// <summary>
        /// Amount of the funds to cashout.
        /// Should be positive number.
        /// </summary>
        [ProtoMember(4)]
        public decimal Amount { get; set; }
    }
}
