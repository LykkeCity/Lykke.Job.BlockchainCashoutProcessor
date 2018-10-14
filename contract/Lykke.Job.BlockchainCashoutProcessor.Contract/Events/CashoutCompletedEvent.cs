using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
    /// <summary>
    /// This event is published, when the client's withdrawal of the funds to an external blockchain
    /// address is completed and withdrawals aggregation is disabled for the integration
    /// </summary>
    /// <remarks>
    /// There are three kind of withdrawals and each kind has its own event to indicate a completion:
    /// 1. Withdrawal to the external address without aggregation - <see cref="CashoutCompletedEvent"/>
    /// 2. Withdrawal to the external address with aggregation - <see cref="CashoutsBatchCompletedEvent"/>
    /// 3. Withdrawal to the deposit address of the another Lykke user - <see cref="CrossClientCashoutCompletedEvent"/>.
    /// </remarks>
    [PublicAPI]
    [MessagePackObject(keyAsPropertyName: true)]
    public class CashoutCompletedEvent
    {
        public string AssetId { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public decimal TransactionAmount { get; set; }
        public decimal TransactionFee { get; set; }
        public Guid ClientId { get; set; }
        public Guid OperationId { get; set; }
        public string TransactionHash { get; set; }
        public DateTime StartMoment { get; set; }
        public DateTime FinishMoment { get; set; }
    }
}
