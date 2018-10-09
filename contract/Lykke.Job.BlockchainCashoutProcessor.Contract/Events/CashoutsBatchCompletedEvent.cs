using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
    /// <summary>
    /// This event is published, when batch of the cashouts is successfully completed. This event could include one or more user cashouts,
    /// it depends on cashout parameters of the blockchain integration configuration and capabilities. If blockchain integration doesn't support
    /// cashouts batching, then <see cref="CashoutCompletedEvent"/> will be published for each completed cashout.
    /// </summary>
    [PublicAPI]
    [MessagePackObject(keyAsPropertyName:true)]
    public class CashoutsBatchCompletedEvent
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
        /// Blockchain transaction hash. Could be dummy "0x" if cashout is executed without blockchain transaction.
        /// This could be cross-client cashout, when client A withdrawing funds to the deposit wallet of the client B
        /// </summary>
        public string TransactionHash { get; set; }

        /// <summary>
        /// Cashouts that were completed
        /// </summary>
        public BatchedCashout[] Cashouts { get; set; }
    }
}
