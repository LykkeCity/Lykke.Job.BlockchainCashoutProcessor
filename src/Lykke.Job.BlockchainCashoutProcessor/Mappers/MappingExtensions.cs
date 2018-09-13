using System;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashoutProcessor.Mappers
{
    public static class MappingExtensions
    {
        public static CashoutFailCode MapToCashoutProcessResult(this OperationExecutionErrorCode source)
        {
            switch (source)
            {
                case OperationExecutionErrorCode.Unknown:
                    return CashoutFailCode.Unknown;
                case OperationExecutionErrorCode.AmountTooSmall:
                    return CashoutFailCode.AmountTooSmall;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }


        public static CashoutErrorCode MapToCashoutProcessErrorCode(this CashoutFailCode source)
        {
            switch (source)
            {
                case CashoutFailCode.Unknown:
                    return CashoutErrorCode.Unknown;
                case CashoutFailCode.AmountTooSmall:
                    return CashoutErrorCode.AmountTooSmall;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }
    }
}
