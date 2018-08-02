﻿using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
    [MessagePackObject]
    public class CashoutStartedEvent
    {
        [Key(0)]
        public Guid OperationId { get; set; }

        [Key(1)]
        public string BlockchainType { get; set; }

        [Key(2)]
        public string BlockchainAssetId { get; set; }

        /// <summary>
        /// Lykke asset ID.
        /// </summary>
        [Key(3)]
        public string AssetId { get; set; }

        [Key(4)]
        public string HotWalletAddress { get; set; }

        [Key(5)]
        public string ToAddress { get; set; }

        [Key(6)]
        public decimal Amount { get; set; }

        [Key(7)]
        public Guid ClientId { get; set; }
    }
}