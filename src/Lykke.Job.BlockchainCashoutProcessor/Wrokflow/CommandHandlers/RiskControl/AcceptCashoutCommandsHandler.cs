using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services;
using Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings;
using Lykke.Job.BlockchainCashoutProcessor.StateMachine;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.RiskControl;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.RiskControl
{
    public class AcceptCashoutCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ICashoutsBatchRepository _cashoutsBatchRepository;
        private readonly IClosedBatchedCashoutRepository _closedBatchedCashoutRepository;
        private readonly IActiveCashoutsBatchIdRepository _activeCashoutsBatchIdRepository;
        private readonly IBlockchainConfigurationsProvider _blockchainConfigurationProvider;
        private readonly IBlockchainWalletsClient _walletsClient;
        private readonly CqrsSettings _cqrsSettings;
        private readonly bool _disableDirectCrossClientCashouts;

        public AcceptCashoutCommandsHandler(
            IChaosKitty chaosKitty,
            ICashoutsBatchRepository cashoutsBatchRepository,
            IClosedBatchedCashoutRepository closedBatchedCashoutRepository,
            IActiveCashoutsBatchIdRepository activeCashoutsBatchIdRepository,
            IBlockchainConfigurationsProvider blockchainConfigurationProvider,
            IBlockchainWalletsClient walletsClient,
            CqrsSettings cqrsSettings,
            bool disableDirectCrossClientCashouts)
        {
            _chaosKitty = chaosKitty;
            _cashoutsBatchRepository = cashoutsBatchRepository;
            _closedBatchedCashoutRepository = closedBatchedCashoutRepository;
            _activeCashoutsBatchIdRepository = activeCashoutsBatchIdRepository;
            _blockchainConfigurationProvider = blockchainConfigurationProvider ?? throw new ArgumentNullException(nameof(blockchainConfigurationProvider));
            _walletsClient = walletsClient ?? throw new ArgumentNullException(nameof(walletsClient));
            _cqrsSettings = cqrsSettings;
            _disableDirectCrossClientCashouts = disableDirectCrossClientCashouts;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(AcceptCashoutCommand command, IEventPublisher publisher)
        {
            var blockchainConfiguration = _blockchainConfigurationProvider.GetConfiguration(command.BlockchainType);

            var recipientClientId = !_disableDirectCrossClientCashouts
                ? await _walletsClient.TryGetClientIdAsync(command.BlockchainType, command.ToAddress)
                : null;

            if (recipientClientId.HasValue)
            {
                return StartCrossClientCashaout(command, publisher, recipientClientId.Value);
            }

            if (blockchainConfiguration.SupportCashoutAggregation)
            {
                return await StartBatchedCashoutAsync(command, publisher, blockchainConfiguration.CashoutsAggregation);
            }

            return StartRegularCashout(command, publisher);
        }

        private async Task<CommandHandlingResult> StartBatchedCashoutAsync(
            AcceptCashoutCommand command,
            IEventPublisher publisher,
            CashoutsAggregationConfiguration aggregationConfiguration)
        {
            if (await _closedBatchedCashoutRepository.IsCashoutClosedAsync(command.OperationId))
            {
                return CommandHandlingResult.Ok();
            }

            var activeCashoutBatchId = await _activeCashoutsBatchIdRepository.GetActiveOrNextBatchId
            (
                command.BlockchainType,
                command.BlockchainAssetId,
                command.HotWalletAddress,
                CashoutsBatchAggregate.GetNextId
            );

            _chaosKitty.Meow(command.OperationId);

            var batch = await _cashoutsBatchRepository.GetOrAddAsync
            (
                activeCashoutBatchId.BatchId,
                () => CashoutsBatchAggregate.Start
                (
                    activeCashoutBatchId.BatchId,
                    command.BlockchainType,
                    command.AssetId,
                    command.BlockchainAssetId,
                    command.HotWalletAddress,
                    aggregationConfiguration.CountThreshold,
                    aggregationConfiguration.AgeThreshold
                )
            );

            _chaosKitty.Meow(command.OperationId);

            var cashout = new BatchedCashoutValueType(command.OperationId, command.ClientId, command.ToAddress, command.Amount);

            var isCashoutShouldWaitForNextBatch = !(batch.IsStillFillingUp || batch.Cashouts.Contains(cashout));

            if (isCashoutShouldWaitForNextBatch)
            {
                return CommandHandlingResult.Fail(_cqrsSettings.RetryDelay);
            }

            var transitionResult = batch.AddCashout(cashout);

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashoutsBatchRepository.SaveAsync(batch);

                _chaosKitty.Meow(command.OperationId);
            }

            if (transitionResult.ShouldPublishEvents())
            {
                if (batch.State == CashoutsBatchState.FillingUp && batch.Cashouts.Count == 1)
                {
                    publisher.PublishEvent
                    (
                        new BatchFillingStartedEvent
                        {
                            BatchId = batch.BatchId
                        }
                    );
                }
                else if (batch.State == CashoutsBatchState.Filled)
                {
                    publisher.PublishEvent
                    (
                        new BatchFilledEvent
                        {
                            BatchId = batch.BatchId
                        }
                    );
                }

                _chaosKitty.Meow(command.OperationId);

                publisher.PublishEvent
                (
                    new BatchedCashoutStartedEvent
                    {
                        BatchId = batch.BatchId,
                        OperationId = command.OperationId,
                        BlockchainType = command.BlockchainType,
                        BlockchainAssetId = command.BlockchainAssetId,
                        AssetId = command.AssetId,
                        HotWalletAddress = command.HotWalletAddress,
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
            AcceptCashoutCommand command,
            IEventPublisher publisher)
        {
            publisher.PublishEvent(new CashoutStartedEvent
            {
                OperationId = command.OperationId,
                BlockchainType = command.BlockchainType,
                BlockchainAssetId = command.BlockchainAssetId,
                HotWalletAddress = command.HotWalletAddress,
                ToAddress = command.ToAddress,
                AssetId = command.AssetId,
                Amount = command.Amount,
                ClientId = command.ClientId
            });

            return CommandHandlingResult.Ok();
        }

        private static CommandHandlingResult StartCrossClientCashaout(
            AcceptCashoutCommand command,
            IEventPublisher publisher,
            Guid recipientClientId)
        {
            publisher.PublishEvent(new CrossClientCashoutStartedEvent
            {
                OperationId = command.OperationId,
                BlockchainType = command.BlockchainType,
                BlockchainAssetId = command.BlockchainAssetId,
                ToAddress = command.ToAddress,
                HotWalletAddress = command.HotWalletAddress,
                AssetId = command.AssetId,
                Amount = command.Amount,
                ClientId = command.ClientId,
                RecipientClientId = recipientClientId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
