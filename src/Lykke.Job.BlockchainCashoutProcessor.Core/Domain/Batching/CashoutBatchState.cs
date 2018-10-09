namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public enum CashoutBatchState
    {
        FillingUp,
        Expired,
        Closed,
        IdRevoked,
        Finished
    }
}
