using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;

namespace Lykke.Job.BlockchainCashoutProcessor.Services.Blockchains
{
    [UsedImplicitly]
    public class BlockchainConfigurationsProvider : IBlockchainConfigurationsProvider
    {
        private readonly IReadOnlyDictionary<string, BlockchainConfiguration> _map;

        public BlockchainConfigurationsProvider(IReadOnlyDictionary<string, BlockchainConfiguration> map)
        {
            _map = map;
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
