using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batch
{
    [MessagePackObject]
    public class CloseActiveBatchCommand
    {
        [Key(0)]
        public Guid BatchId { get; set; }

        [Key(1)]
        public string BlockchainType { get; set; }

        [Key(2)]
        public string BlockchainAssetId { get; set; }

        [Key(3)]
        public string HotWalletAddress { get; set; }
    }
}
