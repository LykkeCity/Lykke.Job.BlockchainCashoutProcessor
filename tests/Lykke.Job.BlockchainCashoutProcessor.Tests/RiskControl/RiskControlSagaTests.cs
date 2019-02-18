using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.RiskControl;
using Lykke.Job.BlockchainCashoutProcessor.Services;
using Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Regular;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.RiskControl;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Regular;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.RiskControl;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.RiskControl;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas;
using Lykke.Job.BlockchainRiskControl.Contract;
using Lykke.Job.BlockchainRiskControl.Contract.Commands;
using Lykke.Job.BlockchainRiskControl.Contract.Events;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainWallets.Client;
using Moq;
using Xunit;

namespace Lykke.Job.BlockchainCashoutProcessor.Tests.RiskControl
{
    public class RiskControlSagaTests
    {
        private readonly ILogFactory _logFactory;
        private readonly Mock<IChaosKitty> _chaosKittyMock;
        private readonly Mock<IEventPublisher> _eventsPublisherMock;
        private readonly Mock<ICommandSender> _commandSender;
        private readonly Mock<ICashoutRiskControlRepository> _cashoutRiskControlRepositoryMock;
        private readonly Mock<ICashoutsBatchRepository> _batchRepositoryMock;
        private readonly Mock<IClosedBatchedCashoutRepository> _closedBatchedCashoutsRepositoryMock;
        private readonly Mock<IActiveCashoutsBatchIdRepository> _activeCashoutsBatchIdRepositoryMock;
        private readonly Mock<IAssetsServiceWithCache> _assetsServiceMock;
        private readonly Mock<IBlockchainWalletsClient> _walletsClientMock;
        private readonly CqrsSettings _cqrsSettings;
        private readonly BlockchainConfigurationsProvider _blockchainConfigurationProvider;
        private readonly StartCashoutCommandsHandler _startCashoutCommandsHandler;
        private readonly NotifyCashoutFailedCommandsHandler _notifyCashoutFailedCommandsHandler;
        private readonly AcceptCashoutCommandsHandler _acceptCashoutCommandsHandler;
        private readonly Asset _asset;
        private readonly RiskControlSaga _saga;

        private CashoutRiskControlAggregate _aggregate;

        public RiskControlSagaTests()
        {
            _logFactory = LogFactory.Create().AddUnbufferedConsole();
            _chaosKittyMock = new Mock<IChaosKitty>();
            _eventsPublisherMock = new Mock<IEventPublisher>();
            _commandSender = new Mock<ICommandSender>();
            _cashoutRiskControlRepositoryMock = new Mock<ICashoutRiskControlRepository>();
            _batchRepositoryMock = new Mock<ICashoutsBatchRepository>();
            _closedBatchedCashoutsRepositoryMock = new Mock<IClosedBatchedCashoutRepository>();
            _activeCashoutsBatchIdRepositoryMock = new Mock<IActiveCashoutsBatchIdRepository>();
            _assetsServiceMock = new Mock<IAssetsServiceWithCache>();
            _walletsClientMock = new Mock<IBlockchainWalletsClient>();
            _cqrsSettings = new CqrsSettings
            {
                RabbitConnectionString = "fake-connection-string",
                RetryDelay = TimeSpan.FromSeconds(30)
            };
            _blockchainConfigurationProvider = new BlockchainConfigurationsProvider
            (
                _logFactory,
                new Dictionary<string, BlockchainConfiguration>
                {
                    { "Bitcoin", new BlockchainConfiguration("HotWallet", false, null) }
                }
            );

             _asset = new Asset
            {
                Id = "LykkeBTC",
                BlockchainIntegrationLayerId = "Bitcoin",
                BlockchainIntegrationLayerAssetId = "BTC"
            };

            _aggregate = null;

            _cashoutRiskControlRepositoryMock
                .Setup(x => x.GetOrAddAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Func<CashoutRiskControlAggregate>>()))
                .ReturnsAsync((Guid opId, Func<CashoutRiskControlAggregate> factory) => _aggregate ?? (_aggregate = factory()));

            _cashoutRiskControlRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<CashoutRiskControlAggregate>()))
                .Callback((CashoutRiskControlAggregate agg) => _aggregate = agg)
                .Returns(Task.CompletedTask);

            _cashoutRiskControlRepositoryMock
                .Setup(x => x.TryGetAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid opId) => opId == _aggregate?.OperationId ? _aggregate : null);

            _assetsServiceMock
                .Setup(x => x.TryGetAssetAsync(
                    It.Is<string>(p => p == "LykkeBTC"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _asset);

            _walletsClientMock
                .Setup(x => x.TryGetClientIdAsync(
                    It.Is<string>(p => p == "Bitcoin"),
                    It.IsAny<string>()))
                .ReturnsAsync((Guid?)null);

            _startCashoutCommandsHandler = new StartCashoutCommandsHandler(_logFactory, _blockchainConfigurationProvider, _assetsServiceMock.Object);

            _notifyCashoutFailedCommandsHandler = new NotifyCashoutFailedCommandsHandler();

            _acceptCashoutCommandsHandler = new AcceptCashoutCommandsHandler(
                _chaosKittyMock.Object,
                _batchRepositoryMock.Object,
                _closedBatchedCashoutsRepositoryMock.Object,
                _activeCashoutsBatchIdRepositoryMock.Object,
                _blockchainConfigurationProvider,
                _walletsClientMock.Object,
                _cqrsSettings,
                false);

            _saga = new RiskControlSaga(_chaosKittyMock.Object, _cashoutRiskControlRepositoryMock.Object);

            _eventsPublisherMock.Setup(x => x.PublishEvent(It.IsAny<ValidationStartedEvent>()))
                .Callback((object evt) => _saga.Handle((ValidationStartedEvent)evt, _commandSender.Object));

            _eventsPublisherMock.Setup(x => x.PublishEvent(It.IsAny<OperationAcceptedEvent>()))
                .Callback((object evt) => _saga.Handle((OperationAcceptedEvent)evt, _commandSender.Object));

            _eventsPublisherMock.Setup(x => x.PublishEvent(It.IsAny<OperationRejectedEvent>()))
                .Callback((object evt) => _saga.Handle((OperationRejectedEvent)evt, _commandSender.Object));

            _commandSender
                .Setup(x => x.SendCommand(
                    It.IsAny<AcceptCashoutCommand>(),
                    It.Is<string>(v => v == BlockchainCashoutProcessorBoundedContext.Name),
                    It.IsAny<uint>()))
                .Callback((AcceptCashoutCommand cmd, string bc, uint _) => _acceptCashoutCommandsHandler.Handle(cmd, _eventsPublisherMock.Object));

            _commandSender
                .Setup(x => x.SendCommand(
                    It.IsAny<NotifyCashoutFailedCommand>(),
                    It.Is<string>(v => v == BlockchainCashoutProcessorBoundedContext.Name),
                    It.IsAny<uint>()))
                .Callback((NotifyCashoutFailedCommand cmd, string bc, uint _) => _notifyCashoutFailedCommandsHandler.Handle(cmd, _eventsPublisherMock.Object));
        }

        [Fact]
        public async Task ShouldStartCashout()
        {
            _commandSender
                .Setup(x => x.SendCommand(
                    It.IsAny<ValidateOperationCommand>(),
                    It.Is<string>(v => v == BlockchainRiskControlBoundedContext.Name),
                    It.IsAny<uint>()))
                .Callback((ValidateOperationCommand cmd, string bc, uint _) => _eventsPublisherMock.Object.PublishEvent(new OperationAcceptedEvent { OperationId = cmd.OperationId }));

            await _startCashoutCommandsHandler.Handle
            (
                new StartCashoutCommand { OperationId = Guid.NewGuid(), ClientId = Guid.NewGuid(), AssetId = _asset.Id, Amount = 0.5m, ToAddress = "TestTo" },
                _eventsPublisherMock.Object
            );

            _eventsPublisherMock.Verify(
                x => x.PublishEvent(It.IsAny<ValidationStartedEvent>()),
                Times.Once);

            _commandSender.Verify(
                x => x.SendCommand(
                    It.IsAny<ValidateOperationCommand>(),
                    It.Is<string>(v => v == BlockchainRiskControlBoundedContext.Name),
                    It.IsAny<uint>()),
                Times.Once);

            _commandSender.Verify(
                x => x.SendCommand(
                    It.IsAny<AcceptCashoutCommand>(),
                    It.Is<string>(v => v == BlockchainCashoutProcessorBoundedContext.Name),
                    It.IsAny<uint>()),
                Times.Once);

            _eventsPublisherMock.Verify(
                x => x.PublishEvent(It.IsAny<CashoutStartedEvent>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldRejectCashout()
        {
            _commandSender
                .Setup(x => x.SendCommand(
                    It.IsAny<ValidateOperationCommand>(),
                    It.Is<string>(v => v == BlockchainRiskControlBoundedContext.Name),
                    It.IsAny<uint>()))
                .Callback((ValidateOperationCommand cmd, string bc, uint _) => _eventsPublisherMock.Object.PublishEvent(new OperationRejectedEvent { OperationId = cmd.OperationId, Message = "TestError" }));

            await _startCashoutCommandsHandler.Handle
            (
                new StartCashoutCommand { OperationId = Guid.NewGuid(), ClientId = Guid.NewGuid(), AssetId = _asset.Id, Amount = 0.5m, ToAddress = "TestTo" },
                _eventsPublisherMock.Object
            );

            _eventsPublisherMock.Verify(
                x => x.PublishEvent(It.IsAny<ValidationStartedEvent>()),
                Times.Once);

            _commandSender.Verify(
                x => x.SendCommand(
                    It.IsAny<ValidateOperationCommand>(),
                    It.Is<string>(v => v == BlockchainRiskControlBoundedContext.Name),
                    It.IsAny<uint>()),
                Times.Once);

            _commandSender.Verify(
                x => x.SendCommand(
                    It.IsAny<NotifyCashoutFailedCommand>(),
                    It.Is<string>(v => v == BlockchainCashoutProcessorBoundedContext.Name),
                    It.IsAny<uint>()),
                Times.Once);

            _eventsPublisherMock.Verify(
                x => x.PublishEvent(It.IsAny<CashoutFailedEvent>()),
                Times.Once);
        }
    }
}