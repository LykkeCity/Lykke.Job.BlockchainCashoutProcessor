using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;

namespace Lykke.Job.BlockchainCashoutProcessor.ContractMapping
{
    public static class BatchedCashoutMappingExtensions
    {
        public static BatchedCashout FromDomain(this BatchedCashoutValueType domain)
        {
            return new BatchedCashout
            {
                OperationId = domain.OperationId,
                DestinationAddress = domain.DestinationAddress,
                Amount = domain.Amount
            };
        }

        public static BatchedCashoutValueType ToDmoain(this BatchedCashout contract)
        {
            return new BatchedCashoutValueType(contract.OperationId, contract.DestinationAddress, contract.Amount);
        }
    }
}
