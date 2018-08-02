using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    /// <summary>Batched Operation execution is completed</summary>
    [MessagePackObject(false)]
    public class BatchedOperationExecutionCompletedEvent
    {
        /// <summary>Lykke unique operation ID</summary>
        [Key(0)]
        public Guid OperationId { get; set; }

        /// <summary>Hash of the blockchain transaction</summary>
        [Key(1)]
        public string TransactionHash { get; set; }

        /// <summary>Actual fee of the operation</summary>
        [Key(2)]
        public Decimal Fee { get; set; }

        /// <summary>
        /// Actual underlying transaction amount.
        /// Single transaction can include multiple operations,
        /// so this value can include multiple operations amount
        /// </summary>
        [Key(3)]
        public Decimal TransactionAmount { get; set; }

        /// <summary>Number of the block, transaction was included to</summary>
        [Key(4)]
        public long Block { get; set; }
    }
}
