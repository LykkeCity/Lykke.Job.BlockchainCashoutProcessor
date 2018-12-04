using System;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashoutProcessor.Mappers
{
    public static class MappingExtensions
    {
        public static Core.Domain.CashoutErrorCode MapToCashoutErrorCode(this OperationExecutionErrorCode source)
        {
            switch (source)
            {
                case OperationExecutionErrorCode.Unknown:
                    return Core.Domain.CashoutErrorCode.Unknown;
                case OperationExecutionErrorCode.AmountTooSmall:
                    return Core.Domain.CashoutErrorCode.AmountTooSmall;
                case OperationExecutionErrorCode.RebuildingRejected:
                    return Core.Domain.CashoutErrorCode.RebuildingRejected;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }
       

        public static Contract.CashoutErrorCode MapToCashoutProcessErrorCode(this Core.Domain.CashoutErrorCode source)
        {
            switch (source)
            {
                case Core.Domain.CashoutErrorCode.Unknown:
                    return Contract.CashoutErrorCode.Unknown;
                case Core.Domain.CashoutErrorCode.RebuildingRejected:
                    return Contract.CashoutErrorCode.Unknown;
                case Core.Domain.CashoutErrorCode.AmountTooSmall:
                    return Contract.CashoutErrorCode.AmountTooSmall;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }
    }
}
