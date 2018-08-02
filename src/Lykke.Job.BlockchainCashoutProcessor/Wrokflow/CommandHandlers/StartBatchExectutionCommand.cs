using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batch
{
    public class StartBatchExectutionCommand
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

        public DateTime StartedAt { get; set; }
    }
}
