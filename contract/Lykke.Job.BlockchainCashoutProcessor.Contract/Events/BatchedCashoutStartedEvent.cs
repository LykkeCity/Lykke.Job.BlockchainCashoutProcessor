using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
    /// <summary>
    /// This event is published, when the client's withdrawal of the funds to an external blockchain
    /// address is just started and withdrawals aggregation is enabled for the integration,
    /// thus is event indicates that withdrawal is added to the cashouts batch.
    /// </summary>
    /// <remarks>
    /// There are three kind of withdrawals and each kind has its own event to indicate a start:
    /// 1. Withdrawal to the external address without aggregation - <see cref="CashoutStartedEvent"/>
    /// 2. Withdrawal to the external address with aggregation - <see cref="BatchedCashoutStartedEvent"/>
    /// 3. Withdrawal to the deposit address of the another Lykke user - <see cref="CrossClientCashoutStartedEvent"/>.
    /// </remarks>
    [PublicAPI]
    [MessagePackObject(keyAsPropertyName: true)]
    public class BatchedCashoutStartedEvent
    {
        public Guid BatchId { get; set; }
        public Guid OperationId { get; set; }
        public Guid ClientId { get; set; }
        public string BlockchainType { get; set; }
        public string BlockchainAssetId { get; set; }
        public string AssetId { get; set; }
        public string HotWalletAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
    }
}
