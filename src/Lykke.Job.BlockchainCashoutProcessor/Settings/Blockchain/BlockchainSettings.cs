using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain
{
    [UsedImplicitly]
    public class BlockchainSettings
    {
        [Optional]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool IsDisabled { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string Type { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string HotWalletAddress { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        [Optional]
        public bool AreCashoutsDisabled { get; set; }

        [Optional]
        public CashoutAggregationSettings CashoutAggregation { get; set; }
    }
}
