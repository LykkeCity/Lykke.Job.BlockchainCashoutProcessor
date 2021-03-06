﻿using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
    /// <summary>
    /// This event is published, when the client's withdrawal of the funds to the deposit wallet of the another Lykke client is just started.
    /// There is no blockchain transaction executed for this kind of withdrawal.
    /// </summary>
    /// <remarks>
    /// There are three kind of withdrawals and each kind has its own event to indicate a start:
    /// 1. Withdrawal to the external address without aggregation - <see cref="CashoutStartedEvent"/>
    /// 2. Withdrawal to the external address with aggregation - <see cref="BatchedCashoutStartedEvent"/>
    /// 3. Withdrawal to the deposit address of the another Lykke user - <see cref="CrossClientCashoutStartedEvent"/>.
    /// </remarks>
    [PublicAPI]
    [MessagePackObject(keyAsPropertyName: true)]
    public class CrossClientCashoutStartedEvent
    {
        public Guid OperationId { get; set; }
        public string BlockchainType { get; set; }
        public string BlockchainAssetId { get; set; }
        public string AssetId { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public Guid ClientId { get; set; }
        public Guid RecipientClientId { get; set; }
        public string HotWalletAddress { get; set; }
    }
}
