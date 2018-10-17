using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.ContractMapping
{
    public static class BatchedCashoutMappingExtensions
    {
        public static BatchedCashout ToContract(this BatchedCashoutValueType domain)
        {
            return new BatchedCashout
            {
                OperationId = domain.CashoutId,
                ClientId = domain.ClientId,
                ToAddress = domain.ToAddress,
                Amount = domain.Amount
            };
        }
    }
}
