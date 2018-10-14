using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract
{
    /// <summary>
    /// Object that describes particular cashout in the batch
    /// </summary>
    [PublicAPI]
    [MessagePackObject(keyAsPropertyName: true)]
    public class BatchedCashout
    {
        /// <summary>
        /// Cashout operation ID
        /// </summary>
        public Guid OperationId { get; set; }

        /// <summary>
        /// Cashout client ID
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        /// Destination address
        /// </summary>
        public string ToAddress { get; set; }

        /// <summary>
        /// Cashout amount 
        /// </summary>
        public decimal Amount { get; set; }
    }
}
