namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain
{
    public enum TransitionResult
    {
        Switched,
        AlreadyInTargetState,
        AlreadyInFutureState
    }
}