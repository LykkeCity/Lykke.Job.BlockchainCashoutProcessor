using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batch
{
    [MessagePackObject]
    public class CashoutBatchStartedEvent
    {
        [Key(0)]
        public Guid BatchId { get; set; }

        [Key(1)]
        public string BlockchainType { get; set; }

        [Key(2)]
        public string BlockchainAssetId { get; set; }

        [Key(2)]
        public string HotWallet { get; set; }

        [Key(3)]
        public bool IncludeFee { get; set; }
    }
}
