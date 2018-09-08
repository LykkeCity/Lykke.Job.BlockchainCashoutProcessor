using JetBrains.Annotations;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;

namespace Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    internal class OperationOutputEntity
    {
        public string Address { get; set; }
        public decimal Amount { get; set; }

        public static OperationOutputEntity FromDomain(OperationOutputValueType domain)
        {
            return new OperationOutputEntity
            {
                Address = domain.Address,
                Amount = domain.Amount
            };
        }

        public OperationOutputValueType ToDomain()
        {
            return new OperationOutputValueType(Address, Amount);
        }
    }
}
