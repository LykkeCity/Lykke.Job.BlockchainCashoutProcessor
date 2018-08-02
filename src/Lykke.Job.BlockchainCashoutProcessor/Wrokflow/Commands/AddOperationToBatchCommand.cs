using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands
{
    [MessagePackObject()]
    public class AddOperationToBatchCommand
    {
        [Key(0)]
        public Guid OperationId { get; set; }

        [Key(1)]
        public string ToAddress { get; set; }

        [Key(2)]
        public decimal Amount { get; set; }

        [Key(3)]
        public string BlockchainType { get; set; }

        [Key(4)]
        public string BlockchainAssetId { get; set; }

        [Key(5)]
        public string HotWalletAddress { get; set; }
    }
}
