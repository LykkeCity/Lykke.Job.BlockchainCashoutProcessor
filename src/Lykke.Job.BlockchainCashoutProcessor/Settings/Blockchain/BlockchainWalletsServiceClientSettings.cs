using Lykke.SettingsReader.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings.Blockchain
{
    public class BlockchainWalletsServiceClientSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }
}
