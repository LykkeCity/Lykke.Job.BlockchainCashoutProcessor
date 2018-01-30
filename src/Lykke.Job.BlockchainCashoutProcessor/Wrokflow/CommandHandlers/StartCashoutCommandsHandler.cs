using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using Lykke.Service.Assets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class StartCashoutCommandsHandler
    {
        private readonly ILog _log;
        private readonly IHotWalletsProvider _hotWalletProvider;
        private readonly IAssetsServiceWithCache _assetsService;

        public StartCashoutCommandsHandler(
            ILog log,
            IHotWalletsProvider hotWalletProvider,
            IAssetsServiceWithCache assetsService)
        {
            _log = log;
            _hotWalletProvider = hotWalletProvider;
            _assetsService = assetsService;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(StartCashoutCommand command, IEventPublisher publisher)
        {
#if DEBUG
            _log.WriteInfo(nameof(StartCashoutCommand), command, "");
#endif

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

            var hotWaletAddress = _hotWalletProvider.GetHotWalletAddress(asset.BlockchainIntegrationLayerId);

            publisher.PublishEvent(new CashoutStartedEvent
            {
                OperationId = command.OperationId,
                BlockchainType = asset.BlockchainIntegrationLayerId,
                BlockchainAssetId = asset.BlockchainIntegrationLayerAssetId,
                HotWalletAddress = hotWaletAddress,
                ToAddress = command.ToAddress,
                AssetId = command.AssetId,
                Amount = command.Amount,
                ClientId = command.ClientId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
