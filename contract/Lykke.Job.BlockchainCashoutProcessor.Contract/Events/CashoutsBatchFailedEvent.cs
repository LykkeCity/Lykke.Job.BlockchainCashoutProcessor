using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
    /// <summary>
    /// This event is published, when batch of the cashouts is failed. This event could include one or more user cashouts.
    /// This event is depends on cashout parameters of the blockchain integration configuration and its capabilities.
    /// If blockchain integration doesn't support cashouts batching, then <see cref="CashoutFailedEvent"/> will be published
    /// for each completed cashout.
    /// </summary>
    [PublicAPI]
    [MessagePackObject(keyAsPropertyName:true)]
    public class CashoutsBatchFailedEvent
    {
        /// <summary>
        /// Cashouts batch id. In general it's not the same as operation id that was used to start the cashout,
        /// to obtain operation ids, see <see cref="Cashouts"/>
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// Lykke asset ID
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// Cashouts that were completed
        /// </summary>
        public BatchedCashout[] Cashouts { get; set; }

        /// <summary>
        /// Error code
        /// </summary>
        public CashoutErrorCode ErrorCode { get; set; }

        /// <summary>
        /// Error description
        /// </summary>
        public string Error { get; set; }
    }
}
