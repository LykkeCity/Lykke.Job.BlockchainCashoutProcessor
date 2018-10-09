using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract
{
    /// <summary>
    /// Object that describes particular cashout in the batch
    /// </summary>
    [PublicAPI]
    [MessagePackObject(keyAsPropertyName: true)]
    public class BatchedCashout
    {
        public Guid OperationId { get; set; }
        public Guid ClientId { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
    }
}
