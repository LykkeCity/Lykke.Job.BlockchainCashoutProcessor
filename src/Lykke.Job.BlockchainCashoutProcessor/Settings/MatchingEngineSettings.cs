using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings
{
    [UsedImplicitly]
    public class MatchingEngineSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public IpEndpointSettings IpEndpoint { get; set; }
    }
}
