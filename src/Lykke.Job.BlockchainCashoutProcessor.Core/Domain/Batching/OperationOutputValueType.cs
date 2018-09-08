namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public class OperationOutputValueType
    {
        public string Address { get; }

        public decimal Amount { get; }

        public OperationOutputValueType(string address, decimal amount)
        {
            Address = address;
            Amount = amount;
        }
    }
}
