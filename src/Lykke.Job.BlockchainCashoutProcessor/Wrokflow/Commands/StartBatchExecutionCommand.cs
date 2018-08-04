using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands
{
    [MessagePackObject]
    public class StartBatchExecutionCommand
    {
        [Key(0)]
        public Guid BatchId { get; set; }

        [Key(1)]
        public string BlockchainType { get; set; }

        [Key(2)]
        public string BlockchainAssetId { get; set; }

        [Key(3)]
        public string HotWallet { get; set; }

        [Key(4)]
        public DateTime StartedAt { get; set; }
    }
}
