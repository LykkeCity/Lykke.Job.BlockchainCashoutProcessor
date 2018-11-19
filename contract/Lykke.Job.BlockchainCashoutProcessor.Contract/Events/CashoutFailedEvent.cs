﻿using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
    /// <summary>
    /// This event is published, when the client's withdrawal of the funds to an external blockchain
    /// address is failed and withdrawals aggregation is disabled for the integration
    /// </summary>
    /// <remarks>
    /// There are two kind of withdrawals and each kind has its own event to indicate a failure:
    /// 1. Withdrawal to the external address without aggregation - <see cref="CashoutFailedEvent"/>
    /// 2. Withdrawal to the external address with aggregation - <see cref="CashoutsBatchFailedEvent"/>
    /// There is no CrossClientCashoutFailedEvent because there it's always executing to completion.
    /// </remarks>
    [PublicAPI]
    [MessagePackObject(keyAsPropertyName: true)]
    public class CashoutFailedEvent
    {
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public Guid ClientId { get; set; }
        public Guid OperationId { get; set; }
        public string Error { get; set; }
        public CashoutErrorCode ErrorCode { get; set; }
        public DateTime StartMoment { get; set; }
        public DateTime FinishMoment { get; set; }
    }
}
