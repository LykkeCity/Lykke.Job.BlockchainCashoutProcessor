using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
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
        private readonly IBlockchainConfigurationsProvider _blockchainConfigurationProvider;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly IBlockchainWalletsClient _walletsClient;
        private readonly bool _disableDirectCrossClientCashouts;

        public StartCashoutCommandsHandler(
            ILog log,
            IBlockchainConfigurationsProvider blockchainConfigurationProvider,
            IAssetsServiceWithCache assetsService,
            IBlockchainWalletsClient walletsClient,
            bool disableDirectCrossClientCashouts)
        {
            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            _log = log.CreateComponentScope(nameof(StartCashoutCommandsHandler));
            _blockchainConfigurationProvider = blockchainConfigurationProvider ?? throw new ArgumentNullException(nameof(blockchainConfigurationProvider));
            _assetsService = assetsService ?? throw new ArgumentNullException(nameof(assetsService));
            _walletsClient = walletsClient ?? throw new ArgumentNullException(nameof(walletsClient));
            _disableDirectCrossClientCashouts = disableDirectCrossClientCashouts;
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
                _log.WriteInfo(nameof(StartCashoutCommand), command, $"Cashouts for {asset.BlockchainIntegrationLayerId} are disabled");

                return CommandHandlingResult.Fail(TimeSpan.FromHours(1));
            }

            var toAddress = command.ToAddress;
            var recipientClientId = _disableDirectCrossClientCashouts
                ? null
                : await _walletsClient.TryGetClientIdAsync(
                    asset.BlockchainIntegrationLayerId,
                    asset.BlockchainIntegrationLayerAssetId,
                    toAddress);

            if (!recipientClientId.HasValue)
            {
                if (blockchainConfiguration.SupportCashoutAggregation)
                {
                    publisher.PublishEvent(new CashoutBatchingStartedEvent
                    {
                        OperationId = command.OperationId,
                        BlockchainType = asset.BlockchainIntegrationLayerId,
                        BlockchainAssetId = asset.BlockchainIntegrationLayerAssetId,
                        HotWalletAddress = blockchainConfiguration.HotWalletAddress,
                        ToAddress = toAddress,
                        AssetId = command.AssetId,
                        Amount = command.Amount,
                        ClientId = command.ClientId
                    });
                }
                else
                {
                    publisher.PublishEvent(new CashoutStartedEvent
                    {
                        OperationId = command.OperationId,
                        BlockchainType = asset.BlockchainIntegrationLayerId,
                        BlockchainAssetId = asset.BlockchainIntegrationLayerAssetId,
                        HotWalletAddress = blockchainConfiguration.HotWalletAddress,
                        ToAddress = toAddress,
                        AssetId = command.AssetId,
                        Amount = command.Amount,
                        ClientId = command.ClientId
                    });
                }
            }
            else //CrossClient Cashout
            {
                publisher.PublishEvent(new CrossClientCashoutStartedEvent
                {
                    OperationId = command.OperationId,
                    BlockchainType = asset.BlockchainIntegrationLayerId,
                    BlockchainAssetId = asset.BlockchainIntegrationLayerAssetId,
                    ToAddress = toAddress,
                    HotWalletAddress = blockchainConfiguration.HotWalletAddress,
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
