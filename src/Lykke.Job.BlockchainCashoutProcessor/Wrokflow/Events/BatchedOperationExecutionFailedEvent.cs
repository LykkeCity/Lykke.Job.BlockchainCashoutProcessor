using System;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Errors;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    /// <summary>Batched operation execution is failed</summary>
    [MessagePackObject(false)]
    public class BatchedOperationExecutionFailedEvent
    {
        /// <summary>Lykke unique operation ID</summary>
        [Key(0)]
        public Guid OperationId { get; set; }

        /// <summary>Error description</summary>
        [Key(1)]
        public string Error { get; set; }

        /// <summary>Error code</summary>
        [Key(2)]
        public OperationExecutionErrorCode ErrorCode { get; set; }
    }
}
