using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services;
using Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings;
using Lykke.Job.BlockchainCashoutProcessor.StateMachine;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class StartCashoutCommandsHandler
    {
        private readonly ILog _log;
        private readonly IChaosKitty _chaosKitty;
        private readonly ICashoutsBatchRepository _cashoutsBatchRepository;
        private readonly IActiveCashoutsBatchIdRepository _activeCashoutsBatchIdRepository;
        private readonly IBlockchainConfigurationsProvider _blockchainConfigurationProvider;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly IBlockchainWalletsClient _walletsClient;
        private readonly CqrsSettings _cqrsSettings;
        private readonly bool _disableDirectCrossClientCashouts;

        public StartCashoutCommandsHandler(
            ILogFactory logFactory,
            IChaosKitty chaosKitty,
            ICashoutsBatchRepository cashoutsBatchRepository,
            IActiveCashoutsBatchIdRepository activeCashoutsBatchIdRepository,
            IBlockchainConfigurationsProvider blockchainConfigurationProvider,
            IAssetsServiceWithCache assetsService,
            IBlockchainWalletsClient walletsClient,
            CqrsSettings cqrsSettings,
            bool disableDirectCrossClientCashouts)
        {
            _log = logFactory.CreateLog(this);
            _chaosKitty = chaosKitty;
            _cashoutsBatchRepository = cashoutsBatchRepository;
            _activeCashoutsBatchIdRepository = activeCashoutsBatchIdRepository;
            _blockchainConfigurationProvider = blockchainConfigurationProvider ?? throw new ArgumentNullException(nameof(blockchainConfigurationProvider));
            _assetsService = assetsService ?? throw new ArgumentNullException(nameof(assetsService));
            _walletsClient = walletsClient ?? throw new ArgumentNullException(nameof(walletsClient));
            _cqrsSettings = cqrsSettings;
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
                _log.Warning(
                    $"Cashouts for {asset.BlockchainIntegrationLayerId} are disabled",
                    context: command);

                return CommandHandlingResult.Fail(TimeSpan.FromMinutes(10));
            }

            var recipientClientId = _disableDirectCrossClientCashouts
                ? null
                : await _walletsClient.TryGetClientIdAsync(asset.BlockchainIntegrationLayerId, command.ToAddress);

            if (recipientClientId.HasValue)
            {
                return StartCrossClientCashaout
                (
                    command,
                    publisher,
                    asset,
                    blockchainConfiguration,
                    recipientClientId.Value
                );
            }

            if (blockchainConfiguration.SupportCashoutAggregation)
            {
                return await StartBatchedCashoutAsync
                (
                    command,
                    publisher,
                    asset,
                    blockchainConfiguration,
                    blockchainConfiguration.CashoutsAggregation
                );
            }

            return StartRegularCashout
            (
                command,
                publisher,
                asset,
                blockchainConfiguration
            );
        }

        private async Task<CommandHandlingResult> StartBatchedCashoutAsync(
            StartCashoutCommand command, 
            IEventPublisher publisher, 
            Asset asset,
            BlockchainConfiguration blockchainConfiguration,
            CashoutsAggregationConfiguration aggregationConfiguration)
        {
            var blockchainType = asset.BlockchainIntegrationLayerId;
            var blockchainAssetId = asset.BlockchainIntegrationLayerAssetId;

            var activeCashoutBatchId = await _activeCashoutsBatchIdRepository.GetActiveOrNextBatchId
            (
                blockchainType,
                blockchainAssetId,
                blockchainConfiguration.HotWalletAddress,
                CashoutsBatchAggregate.GetNextId
            );
                    
            _chaosKitty.Meow(command.OperationId);

            var batch = await _cashoutsBatchRepository.GetOrAddAsync
            (
                activeCashoutBatchId.BatchId,
                () => CashoutsBatchAggregate.Start
                (
                    activeCashoutBatchId.BatchId,
                    blockchainType,
                    asset.Id,
                    blockchainAssetId,
                    blockchainConfiguration.HotWalletAddress,
                    aggregationConfiguration.CountThreshold,
                    aggregationConfiguration.AgeThreshold
                )
            );

            _chaosKitty.Meow(command.OperationId);
            
            if (!batch.IsStillFillingUp)
            {
                return CommandHandlingResult.Fail(_cqrsSettings.RetryDelay);
            }
            
            var transitionResult = batch.AddCashout
            (
                command.OperationId,
                command.ClientId,
                command.ToAddress,
                command.Amount
            );

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashoutsBatchRepository.SaveAsync(batch);

                _chaosKitty.Meow(command.OperationId);
            }

            if (transitionResult.ShouldPublishEvents())
            {
                publisher.PublishEvent
                (
                    new CashoutAddedToBatchEvent
                    {
                        BatchId = batch.BatchId,
                        CashoutsCount = batch.Cashouts.Count,
                        CashoutsCountThreshold = batch.CountThreshold
                    }
                );

                _chaosKitty.Meow(command.OperationId);

                publisher.PublishEvent
                (
                    new BatchedCashoutStartedEvent
                    {
                        BatchId = batch.BatchId,
                        OperationId = command.OperationId,
                        BlockchainType = batch.BlockchainType,
                        BlockchainAssetId = batch.BlockchainAssetId,
                        AssetId = batch.AssetId,
                        HotWalletAddress = batch.HotWalletAddress,
                        ToAddress = command.ToAddress,
                        Amount = command.Amount,
                        ClientId = command.ClientId
                    }
                );

                _chaosKitty.Meow(command.OperationId);
            }

            return CommandHandlingResult.Ok();
        }

        private static CommandHandlingResult StartRegularCashout(
            StartCashoutCommand command, 
            IEventPublisher publisher, 
            Asset asset,
            BlockchainConfiguration blockchainConfiguration)
        {
            publisher.PublishEvent(new CashoutStartedEvent
            {
                OperationId = command.OperationId,
                BlockchainType = asset.BlockchainIntegrationLayerId,
                BlockchainAssetId = asset.BlockchainIntegrationLayerAssetId,
                HotWalletAddress = blockchainConfiguration.HotWalletAddress,
                ToAddress = command.ToAddress,
                AssetId = command.AssetId,
                Amount = command.Amount,
                ClientId = command.ClientId
            });

            return CommandHandlingResult.Ok();
        }

        private static CommandHandlingResult StartCrossClientCashaout(
            StartCashoutCommand command, 
            IEventPublisher publisher, 
            Asset asset,
            BlockchainConfiguration blockchainConfiguration, 
            Guid recipientClientId)
        {
            publisher.PublishEvent(new CrossClientCashoutStartedEvent
            {
                OperationId = command.OperationId,
                BlockchainType = asset.BlockchainIntegrationLayerId,
                BlockchainAssetId = asset.BlockchainIntegrationLayerAssetId,
                ToAddress = command.ToAddress,
                HotWalletAddress = blockchainConfiguration.HotWalletAddress,
                AssetId = command.AssetId,
                Amount = command.Amount,
                ClientId = command.ClientId,
                RecipientClientId = recipientClientId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
