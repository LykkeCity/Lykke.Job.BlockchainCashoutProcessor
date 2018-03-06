using MessagePack;
using System;

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
        public Guid RecipientClientId { get; set; }
        
        [Key(2)]
        public decimal Amount { get; set; }

        [Key(3)]
        public string AssetId { get; set; }

        [Key(4)]
        public Guid CashinOperationId { get; set; }

    }
}
