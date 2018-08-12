using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;

namespace Lykke.Job.BlockchainCashoutProcessor.ContractMapping
{
    public static class BatchedCashoutMapper
    {
        public static BatchedCashout FromDomain(BatchedCashoutValueType domain)
        {
            return new BatchedCashout
            {
                OperationId = domain.OperationId,
                DestinationAddress = domain.DestinationAddress,
                Amount = domain.Amount
            };
        }

        public static BatchedCashoutValueType ToDmoain(BatchedCashout contract)
        {
            return new BatchedCashoutValueType(contract.OperationId, contract.DestinationAddress, contract.Amount);
        }
    }
}
