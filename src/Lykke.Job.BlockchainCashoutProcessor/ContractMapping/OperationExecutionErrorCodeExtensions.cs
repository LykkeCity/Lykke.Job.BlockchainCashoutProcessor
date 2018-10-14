using System;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashoutProcessor.ContractMapping
{
    public static class OperationExecutionErrorCodeExtensions
    {
        public static CashoutErrorCode ToContract(this OperationExecutionErrorCode source)
        {
            switch (source)
            {
                case OperationExecutionErrorCode.Unknown:
                    return CashoutErrorCode.Unknown;

                case OperationExecutionErrorCode.AmountTooSmall:
                    return CashoutErrorCode.AmountTooSmall;

                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }
    }
}