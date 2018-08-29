using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
    /// <summary>
    /// Cashout process is failed
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class CashoutFailedEvent
    {
        /// <summary>
        ///  Lykke unique asset ID
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Lykke unique client ID
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        /// Lykke unique operation ID
        /// </summary>
        public Guid OperationId { get; set; }

        /// <summary>
        /// Error description
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Error code
        /// </summary>
        public ChashoutErrorCode ErrorCode { get; set; }
    }
}
