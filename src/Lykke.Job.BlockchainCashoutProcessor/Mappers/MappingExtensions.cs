using System;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashoutProcessor.Mappers
{
    public static class MappingExtensions
    {
        public static CashoutResult MapToChashoutProcessResult(this OperationExecutionErrorCode source)
        {
            switch (source)
            {
                case OperationExecutionErrorCode.Unknown:
                    return CashoutResult.Unknown;
                case OperationExecutionErrorCode.AmountTooSmall:
                    return CashoutResult.AmountTooSmall;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }


        public static ChashoutErrorCode MapToChashoutProcessErrorCode(this CashoutResult source)
        {
            switch (source)
            {
                case CashoutResult.Unknown:
                    return ChashoutErrorCode.Unknown;
                case CashoutResult.AmountTooSmall:
                    return ChashoutErrorCode.AmountTooSmall;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }
    }
}
