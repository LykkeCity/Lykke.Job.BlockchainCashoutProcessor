using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;

namespace Lykke.Job.BlockchainCashoutProcessor.StateMachine
{
    public static class TransitionResultExtensions
    {
        public static bool ShouldPublishEvents(this TransitionResult transitionResult)
        {
            return transitionResult == TransitionResult.Switched ||
                   transitionResult == TransitionResult.AlreadyInTargetState;
        }

        public static bool ShouldSaveAggregate(this TransitionResult transitionResult)
        {
            return transitionResult == TransitionResult.Switched;
        }
    }
}
