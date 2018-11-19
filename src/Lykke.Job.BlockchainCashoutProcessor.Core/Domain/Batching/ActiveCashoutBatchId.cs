using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public sealed class ActiveCashoutBatchId
    {
        public Guid BatchId { get; }

        private ActiveCashoutBatchId(Guid batchId)
        {
            BatchId = batchId;
        }

        public static ActiveCashoutBatchId Create(Guid batchId)
        {
            return new ActiveCashoutBatchId(batchId);
        }
    }
}
