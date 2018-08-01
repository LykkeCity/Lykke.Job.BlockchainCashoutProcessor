using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract
{
    /// <summary>
    /// Generic blockchain integration layer cashout batching context constants
    /// </summary>
    [PublicAPI]
    public static class BlockchainCashoutBatchProcessorBoundedContext
    {
        /// <summary>
        /// Context name
        /// </summary>
        public static readonly string Name = "bcn-integration.cashout-batch";
    }
}
