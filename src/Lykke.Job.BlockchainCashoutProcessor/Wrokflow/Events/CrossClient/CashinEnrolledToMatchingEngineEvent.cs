using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.CrossClient
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
