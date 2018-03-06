using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain
{
    public class InvalidAggregateStateException : Exception
    {
        public InvalidAggregateStateException(object currentState, object expectedState, object targetState) :
            base(BuildMessage(currentState, expectedState, targetState))
        {

        }

        private static string BuildMessage(object currentState, object expectedState, object targetState)
        {
            return $"Cashout state can't be switched: {currentState} -> {targetState}. Waiting for the {expectedState} state.";
        }
    }
}
