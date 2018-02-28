using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

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

        [Key(1)]
        public Guid ClientId { get; set; }

        [Key(2)]
        public Guid CashinOperationId { get; set; }
    }
}
