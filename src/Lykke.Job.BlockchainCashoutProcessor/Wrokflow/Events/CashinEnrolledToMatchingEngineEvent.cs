using MessagePack;
using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events
{
    /// <summary>
    /// Cashin is enrolled to the ME
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class CashinEnrolledToMatchingEngineEvent
    {
        public Guid CashoutOperationId { get; set; }
    }
}
