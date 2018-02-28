using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands
{
    /// <summary>
    /// Command to enroll cashin to the client ME account
    /// </summary>
    [MessagePackObject]
    public class EnrollToMatchingEngineCommand
    {
        [Key(0)]
        public Guid CashoutOperationId { get; set; }

        [Key(1)]
        public string BlockchainType { get; set; }

        [Key(2)]
        public string DepositWalletAddress { get; set; }

        [Key(3)]
        public string BlockchainAssetId { get; set; }

        [Key(4)]
        public decimal Amount { get; set; }

        [Key(5)]
        public string AssetId { get; set; }

        [Key(6)]
        public Guid CashinOperationId { get; set; }
    }
}
