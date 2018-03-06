using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class StartCashoutCommandsHandler
    {
        private readonly ILog _log;
        private readonly IHotWalletsProvider _hotWalletProvider;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly IBlockchainWalletsClient _walletsClient;

        public StartCashoutCommandsHandler(
            ILog log,
            IHotWalletsProvider hotWalletProvider,
            IAssetsServiceWithCache assetsService,
            IBlockchainWalletsClient walletsClient)
        {
            _log = log;
            _hotWalletProvider = hotWalletProvider;
            _assetsService = assetsService;
            _walletsClient = walletsClient;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(StartCashoutCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(StartCashoutCommand), command, "");
            
            var asset = await _assetsService.TryGetAssetAsync(command.AssetId);

            if (asset == null)
            {
                throw new InvalidOperationException("Asset not found");
            }

            if (string.IsNullOrWhiteSpace(asset.BlockchainIntegrationLayerId))
            {
                throw new InvalidOperationException("BlockchainIntegrationLayerId of the asset is not configured");
            }

            if (string.IsNullOrWhiteSpace(asset.BlockchainIntegrationLayerAssetId))
            {
                throw new InvalidOperationException("BlockchainIntegrationLayerAssetId of the asset is not configured");
            }

            var toAddress = command.ToAddress;
            var hotWaletAddress = _hotWalletProvider.GetHotWalletAddress(asset.BlockchainIntegrationLayerId);
            var recipientClientId = await _walletsClient.TryGetClientIdAsync(
                asset.BlockchainIntegrationLayerId,
                asset.BlockchainIntegrationLayerAssetId, 
                toAddress);

            if (!recipientClientId.HasValue)
            {
                publisher.PublishEvent(new CashoutStartedEvent
                {
                    OperationId = command.OperationId,
                    BlockchainType = asset.BlockchainIntegrationLayerId,
                    BlockchainAssetId = asset.BlockchainIntegrationLayerAssetId,
                    HotWalletAddress = hotWaletAddress,
                    ToAddress = toAddress,
                    AssetId = command.AssetId,
                    Amount = command.Amount,
                    ClientId = command.ClientId
                });
            }
            else //CrossClient Cashout
            {
                publisher.PublishEvent(new CrossClientCashoutStartedEvent
                {
                    OperationId = command.OperationId,
                    BlockchainType = asset.BlockchainIntegrationLayerId,
                    BlockchainAssetId = asset.BlockchainIntegrationLayerAssetId,
                    ToAddress = toAddress,
                    HotWalletAddress = hotWaletAddress,
                    AssetId = command.AssetId,
                    Amount = command.Amount,
                    FromClientId = command.ClientId,
                    RecipientClientId = recipientClientId.Value
                });
            }

            return CommandHandlingResult.Ok();
        }
    }
}
