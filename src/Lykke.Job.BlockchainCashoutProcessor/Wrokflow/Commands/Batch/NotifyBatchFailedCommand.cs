using System;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Errors;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batch
{
    [MessagePackObject]
    public class NotifyBatchFailedCommand
    {
        /// <summary>Lykke unique operation ID</summary>
        [Key(0)]
        public Guid BatchId { get; set; }

        /// <summary>Error description</summary>
        [Key(1)]
        public string Error { get; set; }

        /// <summary>Error code</summary>
        [Key(2)]
        public OperationExecutionErrorCode ErrorCode { get; set; }

        [Key(5)]
        public (Guid operationId, decimal amount, string destinationAddress)[] ToOperations { get; set; }
    }
}
