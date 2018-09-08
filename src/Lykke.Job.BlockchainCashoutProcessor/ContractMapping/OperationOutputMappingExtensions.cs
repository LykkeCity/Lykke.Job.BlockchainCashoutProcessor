using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashoutProcessor.ContractMapping
{
    public static class OperationOutputMappingExtensions
    {
        public static OperationOutput ToContract(this OperationOutputValueType source)
        {
            return new OperationOutput
            {
                Address = source.Address,
                Amount = source.Amount
            };
        }
        public static OperationOutputValueType ToDomain(this OperationOutput source)
        {
            return new OperationOutputValueType(source.Address, source.Amount);
        }
    }
}
