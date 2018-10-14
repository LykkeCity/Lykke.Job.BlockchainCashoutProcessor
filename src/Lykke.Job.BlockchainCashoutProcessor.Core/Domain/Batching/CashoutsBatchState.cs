namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public enum CashoutsBatchState
    {
        FillingUp,
        Expired,
        Closed,
        IdRevoked,
        Finished
    }
}
