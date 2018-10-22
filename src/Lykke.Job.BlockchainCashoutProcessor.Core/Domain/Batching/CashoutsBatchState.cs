namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public enum CashoutsBatchState
    {
        FillingUp,
        Filled,
        Expired,
        Closed,
        IdRevoked,
        Finished
    }
}
