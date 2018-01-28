using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain
{
    [UsedImplicitly]
    public class BlockchainSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string Type { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string HotWalletAddress { get; set; }
    }
}
