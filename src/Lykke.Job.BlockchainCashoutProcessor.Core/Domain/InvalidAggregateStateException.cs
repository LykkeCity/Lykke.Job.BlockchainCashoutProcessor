using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain
{
    public class InvalidAggregateStateException : Exception
    {
        public InvalidAggregateStateException(CashoutState currentState, CashoutState expectedState, CashoutState targetState) :
            base(BuildMessage(currentState, expectedState, targetState))
        {

        }

        private static string BuildMessage(CashoutState currentState, CashoutState expectedState, CashoutState targetState)
        {
            return $"Cashin state can't be switched: {currentState} -> {targetState}. Waiting for the {expectedState} state.";
        }
    }
}