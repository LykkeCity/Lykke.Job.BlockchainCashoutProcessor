using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services;

namespace Lykke.Job.BlockchainCashoutProcessor.Services
{
    [UsedImplicitly]
    public class BlockchainConfigurationsProvider : IBlockchainConfigurationsProvider
    {
        private readonly IReadOnlyDictionary<string, BlockchainConfiguration> _map;

        public BlockchainConfigurationsProvider(
            ILogFactory logFactory,
            IReadOnlyDictionary<string, BlockchainConfiguration> map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));

            var log = logFactory.CreateLog(this);

            foreach (var blockchainType in _map.Keys)
            {
                var blockchainConfiguration = _map[blockchainType];
                
                log.Info($"Blockchain: {blockchainType} is registered -> \r\nHW: {blockchainConfiguration.HotWalletAddress}");

                if (blockchainConfiguration.AreCashoutsDisabled)
                {
                    log.Warning($"Cashouts for blockchain {blockchainType} are disabled");
                }
            }
        }

        public BlockchainConfiguration GetConfiguration(string blockchainType)
        {
            if (!_map.TryGetValue(blockchainType, out var configuration))
            {
                throw new InvalidOperationException($"Configuration for the blockchain type {blockchainType} is not found");
            }

            return configuration;
        }
    }

    
}
