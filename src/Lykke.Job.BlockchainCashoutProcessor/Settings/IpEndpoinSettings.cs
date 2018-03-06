using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;
using System.Net;

namespace Lykke.Job.BlockchainCashoutProcessor.Settings
{
    [UsedImplicitly]
    public class IpEndpointSettings
    {
        [TcpCheck("Port")]
        [UsedImplicitly]
        public string Host { get; set; }

        [UsedImplicitly]
        public int Port { get; set; }

        public IPEndPoint GetClientIpEndPoint()
        {
            if (IPAddress.TryParse(Host, out var ipAddress))
                return new IPEndPoint(ipAddress, Port);

            var addresses = Dns.GetHostAddressesAsync(Host).Result;
            return new IPEndPoint(addresses[0], Port);
        }
    }
}
