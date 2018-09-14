﻿using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
    [MessagePackObject(keyAsPropertyName:true)]
    public class CashinCompletedEvent
    {
        public string AssetId { get; set; }

        public decimal Amount { get; set; }

        public decimal MeAmount { get; set; }

        public decimal Fee { get; set; }

        public Guid ClientId { get; set; }

        public CashinOperationType OperationType { get; set; }

        public Guid OperationId { get; set; }

        public string TransactionHash { get; set; }
    }
}
