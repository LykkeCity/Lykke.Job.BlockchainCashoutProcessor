using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Contract
{
    /// <summary>
    /// Generic blockchain integration layer cashout context constants
    /// </summary>
    [PublicAPI]
    public static class BlockchainCashoutProcessorBoundedContext
    {
        /// <summary>
        /// Context name
        /// </summary>
        public static readonly string Name = "bcn-integration.cashout";
    }
}
