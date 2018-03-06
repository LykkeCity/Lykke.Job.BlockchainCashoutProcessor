using MessagePack;
using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    /// <summary>
    /// Cashin is enrolled to the ME
    /// </summary>
    [MessagePackObject]
    public class CashinEnrolledToMatchingEngineEvent
    {
        [Key(0)]
        public Guid CashoutOperationId { get; set; }
    }
}
