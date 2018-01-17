using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;

namespace Lykke.Job.BlockchainCashoutProcessor.Services.Blockchains
{
    [UsedImplicitly]
    public class HotWalletsProvider : IHotWalletsProvider
    {
        private readonly IReadOnlyDictionary<string, string> _map;

        public HotWalletsProvider(IReadOnlyDictionary<string, string> map)
        {
            _map = map;
        }

        public string GetHotWalletAddress(string blockchainType)
        {
            if (!_map.TryGetValue(blockchainType, out var address))
            {
                throw new InvalidOperationException($"Hot wallet address for the blockchain type {blockchainType} is not found");
            }

            return address;
        }
    }
}
