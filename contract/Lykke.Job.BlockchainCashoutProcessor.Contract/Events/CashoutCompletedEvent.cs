using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
    /// <summary>
    /// This event is published, when client withdrawed funds to an external address or deposit wallet of another client and
    /// funds are successfully transfered.
    /// When funds are transfered to the deposit wallet of another client (B), then no blockchain transaction is executed and
    /// funds will be enrolled to the client B directly and <see cref="CrossClientCashinCompletedEvent"/> is published.
    /// </summary>
    [PublicAPI]
    [MessagePackObject(keyAsPropertyName:true)]
    public class CashoutCompletedEvent
    {
        public string AssetId { get; set; }
        
        public string ToAddress { get; set; }

        public decimal Amount { get; set; }

        public decimal TransactionAmount { get; set; }

        public decimal TransactionFee { get; set; }

        public Guid ClientId { get; set; }

        public CashoutOperationType OperationType { get; set; }

        public Guid OperationId { get; set; }

        public string TransactionHash { get; set; }

        /// <summary>
        /// moment, when cashout was started
        /// </summary>
        public DateTime StartMoment { get; set; }

        /// <summary>
        /// moment, when cashout was finished
        /// </summary>
        public DateTime FinishMoment { get; set; }
    }
}
