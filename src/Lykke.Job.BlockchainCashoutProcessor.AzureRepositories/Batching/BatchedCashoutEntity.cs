using System;
using JetBrains.Annotations;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    internal class BatchedCashoutEntity
    {
        public Guid OperationId { get; set; }
        public string DestinationAddress { get; set; }
        public decimal Amount { get; set; }

        public static BatchedCashoutEntity FromDomain(BatchedCashoutValueType domain)
        {
            return new BatchedCashoutEntity
            {
                OperationId = domain.OperationId,
                DestinationAddress = domain.DestinationAddress,
                Amount = domain.Amount
            };
        }

        public BatchedCashoutValueType ToDomain()
        {
            return new BatchedCashoutValueType(OperationId, DestinationAddress, Amount);
        }
    }
}
