using System;
using JetBrains.Annotations;
using MessagePack;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract.Events
{
    /// <summary>
    /// This event is published, when client A withdrawing funds to the deposit wallet of the client B
    /// and funds are successfully deposited to the client B.
    /// Cross client cashout is performed without blockchain transaction execution.
    /// </summary>
    [PublicAPI]
    [MessagePackObject(keyAsPropertyName:true)]
    public class CrossClientCashinCompletedEvent
    {
        public string AssetId { get; set; }

        public decimal Amount { get; set; }

        public decimal TransactionAmount { get; set; }

        public decimal TransactionFee { get; set; }

        public Guid ClientId { get; set; }

        public Guid OperationId { get; set; }

        public string TransactionHash { get; set; }
    }
}
