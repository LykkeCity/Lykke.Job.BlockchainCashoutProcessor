using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings
{
    [UsedImplicitly]
    public class OperationsRepositoryServiceClientSettings
    {
        [HttpCheck("/api/isalive")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string ServiceUrl { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public int RequestTimeout { get; set; }
    }
}
