using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings
{
    [UsedImplicitly]
    public class BlockchainCashoutProcessorSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public DbSettings Db { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public CqrsSettings Cqrs { get; set; }

        [Optional]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public ChaosSettings ChaosKitty { get; set; }

        [Optional]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public WorkflowSettings Workflow { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BatchMonitoringSettings BatchMonitoring { get; set; }
    }
}
