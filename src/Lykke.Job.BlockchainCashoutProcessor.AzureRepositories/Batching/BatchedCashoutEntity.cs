using System;
using JetBrains.Annotations;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    internal class BatchedCashoutEntity
    {
        public Guid OperationId { get; set; }
        public Guid ClientId { get; set; }
        public string DestinationAddress { get; set; }
        public decimal Amount { get; set; }
        public int IndexInBatch { get; set; }

        public DateTime AddedToBatchAt { get; set; }


        public static BatchedCashoutEntity FromDomain(BatchedCashoutValueType domain)
        {
            return new BatchedCashoutEntity
            {
                OperationId = domain.CashoutId,
                ClientId = domain.ClientId,
                DestinationAddress = domain.ToAddress,
                Amount = domain.Amount,
                IndexInBatch = domain.IndexInBatch,
                AddedToBatchAt = domain.AddedToBatchAt
            };
        }

        public BatchedCashoutValueType ToDomain()
        {
            return new BatchedCashoutValueType(OperationId, ClientId, DestinationAddress, Amount, IndexInBatch, AddedToBatchAt);
        }
    }
}
