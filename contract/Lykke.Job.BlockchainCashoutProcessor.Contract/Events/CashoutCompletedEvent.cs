using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
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
