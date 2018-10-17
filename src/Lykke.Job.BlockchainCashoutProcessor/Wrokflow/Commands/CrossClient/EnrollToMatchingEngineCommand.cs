using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.CrossClient
{
    /// <summary>
    /// Command to enroll cashin to the client ME account
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class EnrollToMatchingEngineCommand
    {
        public Guid CashoutOperationId { get; set; }
        public Guid RecipientClientId { get; set; }
        public decimal Amount { get; set; }
        public string AssetId { get; set; }
        public Guid CashinOperationId { get; set; }
    }
}
