using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Commands
{
    /// <summary>
    /// Command to start the cashout in the generic blockchain integration layer
    /// </summary>
    [PublicAPI]
    [MessagePackObject(keyAsPropertyName: true)]
    public class StartCashoutCommand
    {
        /// <summary>
        /// Cashout operation ID.
        /// </summary>
        public Guid OperationId { get; set; }

        /// <summary>
        /// Lykke asset ID.
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// Recipient address.
        /// </summary>
        public string ToAddress { get; set; }

        /// <summary>
        /// Amount of the funds to cashout.
        /// Should be positive number.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Lykke client ID.
        /// </summary>
        public Guid ClientId { get; set; }
    }
}
