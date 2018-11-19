using System;
using Lykke.Job.BlockchainOperationsExecutor.Contract;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class FailBatchCommand
    {
        public Guid BatchId { get; set; }
        public OperationExecutionErrorCode ErrorCode { get; set; }
        public string Error { get; set; }
    }
}
