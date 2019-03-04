using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.RiskControl;
using Lykke.Service.Assets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class StartCashoutCommandsHandler
    {
        private readonly ILog _log;
        private readonly IBlockchainConfigurationsProvider _blockchainConfigurationProvider;
        private readonly IAssetsServiceWithCache _assetsService;

        public StartCashoutCommandsHandler(
            ILogFactory logFactory,
            IBlockchainConfigurationsProvider blockchainConfigurationProvider,
            IAssetsServiceWithCache assetsService)
        {
            _log = logFactory.CreateLog(this);
            _blockchainConfigurationProvider = blockchainConfigurationProvider ?? throw new ArgumentNullException(nameof(blockchainConfigurationProvider));
            _assetsService = assetsService ?? throw new ArgumentNullException(nameof(assetsService));
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(StartCashoutCommand command, IEventPublisher publisher)
        {
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

            var blockchainConfiguration = _blockchainConfigurationProvider.GetConfiguration(asset.BlockchainIntegrationLayerId);

            if (blockchainConfiguration.AreCashoutsDisabled)
            {
                _log.Warning(
                    $"Cashouts for {asset.BlockchainIntegrationLayerId} are disabled",
                    context: command);

                return CommandHandlingResult.Fail(TimeSpan.FromMinutes(10));
            }

            publisher.PublishEvent(new ValidationStartedEvent
            {
                OperationId = command.OperationId,
                ClientId = command.ClientId,
                AssetId = asset.Id,
                BlockchainType = asset.BlockchainIntegrationLayerId,
                BlockchainAssetId = asset.BlockchainIntegrationLayerAssetId,
                HotWalletAddress = blockchainConfiguration.HotWalletAddress,
                ToAddress = command.ToAddress,
                Amount = command.Amount
            });

            return CommandHandlingResult.Ok();
        }
    }
}
