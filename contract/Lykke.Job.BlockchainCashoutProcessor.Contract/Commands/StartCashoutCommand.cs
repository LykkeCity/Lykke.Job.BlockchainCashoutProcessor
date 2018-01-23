using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Commands
{
    /// <summary>
    /// Command to start the cashout in the generic blockchain integration layer
    /// </summary>
    [PublicAPI]
    [MessagePackObject]
    public class StartCashoutCommand
    {
        /// <summary>
        /// Cashout operation ID.
        /// </summary>
        [Key(0)]
        public Guid OperationId { get; set; }

        /// <summary>
        /// Lykke asset ID.
        /// </summary>
        [Key(1)]
        public string AssetId { get; set; }

        /// <summary>
        /// Recipient address.
        /// </summary>
        [Key(2)]
        public string ToAddress { get; set; }

        /// <summary>
        /// Amount of the funds to cashout.
        /// Should be positive number.
        /// </summary>
        [Key(3)]
        public decimal Amount { get; set; }
    }
}
