using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings
{
    [UsedImplicitly]
    public class MatchingEngineSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public IpEndpointSettings IpEndpoint { get; set; }
    }
}
