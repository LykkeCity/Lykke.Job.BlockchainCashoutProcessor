using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings
{
    [UsedImplicitly]
    public class WorkflowSettings
    {
        [Optional]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public bool DisableDirectCrossClientCashouts { get; set; }
    }
}