using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Services;
using Lykke.Job.BlockchainCashoutProcessor.Settings.JobSettings;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainWallets.Client;
using Moq;
using Xunit;

namespace Lykke.Job.BlockchainCashoutProcessor.Tests.Batching.CommandHandlers
{
    public class StartCashoutCommandsHandlerTests
    {
        private readonly int _countTreshold;

        private readonly Mock<IEventPublisher> _eventsPublisherMock;
        private readonly Mock<ICashoutsBatchRepository> _batchRepositoryMock;
        private readonly Mock<IClosedBatchedCashoutRepository> _closedBatchedCashoutsRepositoryMock;

        private readonly CqrsSettings _cqrsSettings;
        private readonly StartCashoutCommandsHandler _handler;

        private CashoutsBatchAggregate _batch;

        public StartCashoutCommandsHandlerTests()
        {
            var logFactory = LogFactory.Create().AddUnbufferedConsole();
            
            _eventsPublisherMock = new Mock<IEventPublisher>();
            _batchRepositoryMock = new Mock<ICashoutsBatchRepository>();
            _closedBatchedCashoutsRepositoryMock = new Mock<IClosedBatchedCashoutRepository>();
            
            var activeCashoutsBatchIdRepositoryMock = new Mock<IActiveCashoutsBatchIdRepository>();
            var assetsServiceMock = new Mock<IAssetsServiceWithCache>();
            var walletsClient = new Mock<IBlockchainWalletsClient>();

            _cqrsSettings = new CqrsSettings
            {
                RabbitConnectionString = "fake-connection-string",
                RetryDelay = TimeSpan.FromSeconds(30)
            };

            _countTreshold = 10;
            
            var ageThreshold = TimeSpan.FromMinutes(10);

            var blockchainConfigurationProvider = new BlockchainConfigurationsProvider
            (
                logFactory,
                new Dictionary<string, BlockchainConfiguration>
                {
                    {
                        "Bitcoin", 
                        new BlockchainConfiguration
                        (
                            "HotWallet",
                            false,
                            new CashoutsAggregationConfiguration
                            (
                                ageThreshold,
                                _countTreshold
                            ))
                    }
                }
            );

            _handler = new StartCashoutCommandsHandler
            (
                logFactory,
                new SilentChaosKitty(),
                _batchRepositoryMock.Object,
                _closedBatchedCashoutsRepositoryMock.Object,
                activeCashoutsBatchIdRepositoryMock.Object,
                blockchainConfigurationProvider,
                assetsServiceMock.Object,
                walletsClient.Object,
                _cqrsSettings,
                false
            );

            var activeCashoutsBatchId = ActiveCashoutBatchId.Create(CashoutsBatchAggregate.GetNextId());

            _batch = CashoutsBatchAggregate.Start
            (
                activeCashoutsBatchId.BatchId,
                "Bitcoin",
                "LykkeBTC",
                "BTC",
                "HotWallet",
                10,
                TimeSpan.FromMinutes(10)
            );

            var asset = new Asset
            {
                Id = "LykkeBTC",
                BlockchainIntegrationLayerId = "Bitcoin",
                BlockchainIntegrationLayerAssetId = "BTC"
            };

            _batchRepositoryMock
                .Setup
                (
                    x => x.GetOrAddAsync
                    (
                        It.Is<Guid>(p => p == _batch.BatchId),
                        It.IsAny<Func<CashoutsBatchAggregate>>()
                    )
                )
                .ReturnsAsync(() => _batch);

            _closedBatchedCashoutsRepositoryMock
                .Setup(x => x.IsCashoutClosedAsync(It.Is<Guid>(p => p == _batch.BatchId)))
                .ReturnsAsync(false);

            activeCashoutsBatchIdRepositoryMock
                .Setup
                (
                    x => x.GetActiveOrNextBatchId
                    (
                        It.Is<string>(p => p == "Bitcoin"),
                        It.Is<string>(p => p == "BTC"),
                        It.Is<string>(p => p == "HotWallet"),
                        It.Is<Func<Guid>>(p => p == CashoutsBatchAggregate.GetNextId)
                    )
                )
                .ReturnsAsync(() => activeCashoutsBatchId);

            assetsServiceMock
                .Setup
                (
                    x => x.TryGetAssetAsync
                    (
                        It.Is<string>(p => p == "LykkeBTC"),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(() => asset);

            walletsClient
                .Setup
                (
                    x => x.TryGetClientIdAsync
                    (
                        It.Is<string>(p => p == "Bitcoin"),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync((Guid?)null);
        }

        [Fact]
        public async Task Batch_Filling_Started()
        {
            // Arrange

            var clientId = Guid.NewGuid();
            var cashoutId = Guid.NewGuid();

            // Act

            var handlingResult = await _handler.Handle
            (
                new StartCashoutCommand
                {
                    AssetId = "LykkeBTC",
                    Amount = 100,
                    ClientId = clientId,
                    OperationId = cashoutId,
                    ToAddress = "Destination"
                },
                _eventsPublisherMock.Object
            );

            // Assert

            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.FillingUp, _batch.State);
            Assert.Equal(1, _batch.Cashouts.Count);
            Assert.Equal(cashoutId, _batch.Cashouts.First().CashoutId);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.Is<BatchFillingStartedEvent>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock
                .Verify
                (
                    x => x.PublishEvent
                    (
                        It.Is<BatchedCashoutStartedEvent>(p =>
                            p.BatchId == _batch.BatchId &&
                            p.Amount == 100 &&
                            p.AssetId == "LykkeBTC" &&
                            p.BlockchainAssetId == "BTC" &&
                            p.BlockchainType == "Bitcoin" &&
                            p.ClientId == clientId &&
                            p.HotWalletAddress == "HotWallet" &&
                            p.OperationId == cashoutId &&
                            p.ToAddress == "Destination")
                    ),
                    Times.Once
                );
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Batch_Filling_In_Progress()
        {
            // Arrange

            var clientId = Guid.NewGuid();
            var cashoutId = Guid.NewGuid();

            for (var i = 0; i < _countTreshold - 2; ++i)
            {
                _batch.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), $"Destination-{i}", 100 * i));
            }

            // Act

            var handlingResult = await _handler.Handle
            (
                new StartCashoutCommand
                {
                    AssetId = "LykkeBTC",
                    Amount = 50,
                    ClientId = clientId,
                    OperationId = cashoutId,
                    ToAddress = "Destination-last"
                },
                _eventsPublisherMock.Object
            );

            // Assert

            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.FillingUp, _batch.State);
            Assert.Equal(_countTreshold - 1, _batch.Cashouts.Count);
            Assert.Equal(cashoutId, _batch.Cashouts.Last().CashoutId);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock
                .Verify
                (
                    x => x.PublishEvent
                    (
                        It.Is<BatchedCashoutStartedEvent>(p =>
                            p.BatchId == _batch.BatchId &&
                            p.Amount == 50 &&
                            p.AssetId == "LykkeBTC" &&
                            p.BlockchainAssetId == "BTC" &&
                            p.BlockchainType == "Bitcoin" &&
                            p.ClientId == clientId &&
                            p.HotWalletAddress == "HotWallet" &&
                            p.OperationId == cashoutId &&
                            p.ToAddress == "Destination-last")
                    ),
                    Times.Once
                );
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Batch_Filling_Completed()
        {
            // Arrange

            var clientId = Guid.NewGuid();
            var cashoutId = Guid.NewGuid();

            for (var i = 0; i < _countTreshold - 1; ++i)
            {
                _batch.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), "Destination-{i}", 100 * i));
            }

            // Act

            var handlingResult = await _handler.Handle
            (
                new StartCashoutCommand
                {
                    AssetId = "LykkeBTC",
                    Amount = 50,
                    ClientId = clientId,
                    OperationId = cashoutId,
                    ToAddress = "Destination-last"
                },
                _eventsPublisherMock.Object
            );

            // Assert

            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.Filled, _batch.State);
            Assert.Equal(_countTreshold, _batch.Cashouts.Count);
            Assert.Equal(cashoutId, _batch.Cashouts.Last().CashoutId);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.Is<BatchFilledEvent>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock
                .Verify
                (
                    x => x.PublishEvent
                    (
                        It.Is<BatchedCashoutStartedEvent>(p =>
                            p.BatchId == _batch.BatchId &&
                            p.Amount == 50 &&
                            p.AssetId == "LykkeBTC" &&
                            p.BlockchainAssetId == "BTC" &&
                            p.BlockchainType == "Bitcoin" &&
                            p.ClientId == clientId &&
                            p.HotWalletAddress == "HotWallet" &&
                            p.OperationId == cashoutId &&
                            p.ToAddress == "Destination-last")
                    ),
                    Times.Once
                );
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task No_Cashouts_Added_After_Batch_Filling_Completed()
        {
            // == Step 1 - Complete the filling ==

            // Arrange 1

            var clientId = Guid.NewGuid();
            var cashoutId = Guid.NewGuid();

            for (var i = 0; i < _countTreshold - 1; ++i)
            {
                _batch.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), "Destination-{i}", 100 * i));
            }

            // Act 1

            var handlingResult = await _handler.Handle
            (
                new StartCashoutCommand
                {
                    AssetId = "LykkeBTC",
                    Amount = 50,
                    ClientId = clientId,
                    OperationId = cashoutId,
                    ToAddress = "Destination-last"
                },
                _eventsPublisherMock.Object
            );

            // Assert 1

            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.Filled, _batch.State);
            Assert.Equal(_countTreshold, _batch.Cashouts.Count);
            Assert.Equal(cashoutId, _batch.Cashouts.Last().CashoutId);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.Is<BatchFilledEvent>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock
                .Verify
                (
                    x => x.PublishEvent
                    (
                        It.Is<BatchedCashoutStartedEvent>(p =>
                            p.BatchId == _batch.BatchId &&
                            p.Amount == 50 &&
                            p.AssetId == "LykkeBTC" &&
                            p.BlockchainAssetId == "BTC" &&
                            p.BlockchainType == "Bitcoin" &&
                            p.ClientId == clientId &&
                            p.HotWalletAddress == "HotWallet" &&
                            p.OperationId == cashoutId &&
                            p.ToAddress == "Destination-last")
                    ),
                    Times.Once
                );
            _eventsPublisherMock.VerifyNoOtherCalls();

            // == Step 2 - Process Extra cashout ==

            // Arrange 2

            var extraClientId = Guid.NewGuid();
            var extraCashoutId = Guid.NewGuid();

            _batchRepositoryMock.Invocations.Clear();
            _eventsPublisherMock.Invocations.Clear();

            // Act 2

            var extraHandlingResult = await _handler.Handle
            (
                new StartCashoutCommand
                {
                    AssetId = "LykkeBTC",
                    Amount = 10,
                    ClientId = extraClientId,
                    OperationId = extraCashoutId,
                    ToAddress = "Destination-extra"
                },
                _eventsPublisherMock.Object
            );

            // Assert 2

            Assert.True(extraHandlingResult.Retry);
            Assert.Equal(CashoutsBatchState.Filled, _batch.State);
            Assert.Equal(_countTreshold, _batch.Cashouts.Count);
            Assert.DoesNotContain(_batch.Cashouts, x => x.CashoutId == extraCashoutId);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Never);
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Already_Closed_Cashout_Just_Swalloed()
        {
            // Arrange

            var clientId = Guid.NewGuid();
            var cashoutId = Guid.NewGuid();

            _closedBatchedCashoutsRepositoryMock
                .Setup(x => x.IsCashoutClosedAsync(It.Is<Guid>(p => p == cashoutId)))
                .ReturnsAsync(true);

            // Act

            var handlingResult = await _handler.Handle
            (
                new StartCashoutCommand
                {
                    AssetId = "LykkeBTC",
                    Amount = 100,
                    ClientId = clientId,
                    OperationId = cashoutId,
                    ToAddress = "Destination"
                },
                _eventsPublisherMock.Object
            );

            // Assert

            Assert.False(handlingResult.Retry);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CashoutsBatchAggregate>()), Times.Never);
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Wait_For_The_Next_Batch_If_Current_Batch_Is_Already_Not_Filling()
        {
            // Arrange

            var clientId = Guid.NewGuid();
            var cashoutId = Guid.NewGuid();

            var batchEntity = CashoutsBatchEntity.FromDomain(_batch);

            batchEntity.State = CashoutsBatchState.Filled;

            _batch = batchEntity.ToDomain();

            // Act

            var handlingResult = await _handler.Handle
            (
                new StartCashoutCommand
                {
                    AssetId = "LykkeBTC",
                    Amount = 100,
                    ClientId = clientId,
                    OperationId = cashoutId,
                    ToAddress = "Destination"
                },
                _eventsPublisherMock.Object
            );

            // Assert

            Assert.True(handlingResult.Retry);
            Assert.Equal(_cqrsSettings.RetryDelay, TimeSpan.FromMilliseconds(handlingResult.RetryDelay));

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CashoutsBatchAggregate>()), Times.Never);
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Batch_Filling_Started_With_Batch_Saving_Failure()
        {
            // == Step 1 - Failure ==

            // Arrange 1

            var clientId = Guid.NewGuid();
            var cashoutId = Guid.NewGuid();

            _batchRepositoryMock
                .Setup(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)))
                .Throws<InvalidOperationException>();

            // Act/Assert 1

            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle
            (
                new StartCashoutCommand
                {
                    AssetId = "LykkeBTC",
                    Amount = 100,
                    ClientId = clientId,
                    OperationId = cashoutId,
                    ToAddress = "Destination"
                },
                _eventsPublisherMock.Object
            ));

            // Assert 1

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.VerifyNoOtherCalls();

            // == Step 2 - Success ==

            // Arrange 2

            _batchRepositoryMock
                .Setup(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)))
                .Callback(() => { });
            _batchRepositoryMock.Invocations.Clear();

            // Act 2

            var handlingResult = await _handler.Handle
            (
                new StartCashoutCommand
                {
                    AssetId = "LykkeBTC",
                    Amount = 100,
                    ClientId = clientId,
                    OperationId = cashoutId,
                    ToAddress = "Destination"
                },
                _eventsPublisherMock.Object
            );

            // Assert 2
             
            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.FillingUp, _batch.State);
            Assert.Equal(1, _batch.Cashouts.Count);
            Assert.Equal(cashoutId, _batch.Cashouts.First().CashoutId);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CashoutsBatchAggregate>()), Times.Never);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.Is<BatchFillingStartedEvent>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock
                .Verify
                (
                    x => x.PublishEvent
                    (
                        It.Is<BatchedCashoutStartedEvent>(p =>
                            p.BatchId == _batch.BatchId &&
                            p.Amount == 100 &&
                            p.AssetId == "LykkeBTC" &&
                            p.BlockchainAssetId == "BTC" &&
                            p.BlockchainType == "Bitcoin" &&
                            p.ClientId == clientId &&
                            p.HotWalletAddress == "HotWallet" &&
                            p.OperationId == cashoutId &&
                            p.ToAddress == "Destination")
                    ),
                    Times.Once
                );
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Batch_Filling_In_Progress_With_Batch_Saving_Failure()
        {
            // == Step 1 - Failure ==

            // Arrange 1

            var clientId = Guid.NewGuid();
            var cashoutId = Guid.NewGuid();
            var command = new StartCashoutCommand
            {
                AssetId = "LykkeBTC",
                Amount = 50,
                ClientId = clientId,
                OperationId = cashoutId,
                ToAddress = "Destination-last"
            };

            for (var i = 0; i < _countTreshold - 2; ++i)
            {
                _batch.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), $"Destination-{i}", 100 * i));
            }

            _batchRepositoryMock
                .Setup(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)))
                .Throws<InvalidOperationException>();

            // Act/Assert 1

            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, _eventsPublisherMock.Object));

            // Assert 1

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.VerifyNoOtherCalls();

            // == Step 2 - Success ==

            // Arrange 2

            _batchRepositoryMock
                .Setup(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)))
                .Callback(() => { });
            _batchRepositoryMock.Invocations.Clear();

            // Act 2

            var handlingResult = await _handler.Handle(command, _eventsPublisherMock.Object);

            // Assert 2

            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.FillingUp, _batch.State);
            Assert.Equal(_countTreshold - 1, _batch.Cashouts.Count);
            Assert.Equal(cashoutId, _batch.Cashouts.Last().CashoutId);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CashoutsBatchAggregate>()), Times.Never);
            _eventsPublisherMock
                .Verify
                (
                    x => x.PublishEvent
                    (
                        It.Is<BatchedCashoutStartedEvent>(p =>
                            p.BatchId == _batch.BatchId &&
                            p.Amount == 50 &&
                            p.AssetId == "LykkeBTC" &&
                            p.BlockchainAssetId == "BTC" &&
                            p.BlockchainType == "Bitcoin" &&
                            p.ClientId == clientId &&
                            p.HotWalletAddress == "HotWallet" &&
                            p.OperationId == cashoutId &&
                            p.ToAddress == "Destination-last")
                    ),
                    Times.Once
                );
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Batch_Filling_Completed_With_Batch_Saving_Failure()
        {
            // == Step 1 - Failure ==

            // Arrange 1

            var clientId = Guid.NewGuid();
            var cashoutId = Guid.NewGuid();
            var command = new StartCashoutCommand
            {
                AssetId = "LykkeBTC",
                Amount = 50,
                ClientId = clientId,
                OperationId = cashoutId,
                ToAddress = "Destination-last"
            };

            for (var i = 0; i < _countTreshold - 1; ++i)
            {
                _batch.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), "Destination-{i}", 100 * i));
            }

            _batchRepositoryMock
                .Setup(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)))
                .Throws<InvalidOperationException>();

            // Act/Assert 1

            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, _eventsPublisherMock.Object));

            // Assert 1

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.VerifyNoOtherCalls();

            // == Step 2 - Success ==

            // Arrange 2

            _batchRepositoryMock
                .Setup(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)))
                .Callback(() => { });
            _batchRepositoryMock.Invocations.Clear();

            // Act 2

            var handlingResult = await _handler.Handle(command, _eventsPublisherMock.Object);

            // Assert 2

            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.Filled, _batch.State);
            Assert.Equal(_countTreshold, _batch.Cashouts.Count);
            Assert.Equal(cashoutId, _batch.Cashouts.Last().CashoutId);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CashoutsBatchAggregate>()), Times.Never);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.Is<BatchFilledEvent>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock
                .Verify
                (
                    x => x.PublishEvent
                    (
                        It.Is<BatchedCashoutStartedEvent>(p =>
                            p.BatchId == _batch.BatchId &&
                            p.Amount == 50 &&
                            p.AssetId == "LykkeBTC" &&
                            p.BlockchainAssetId == "BTC" &&
                            p.BlockchainType == "Bitcoin" &&
                            p.ClientId == clientId &&
                            p.HotWalletAddress == "HotWallet" &&
                            p.OperationId == cashoutId &&
                            p.ToAddress == "Destination-last")
                    ),
                    Times.Once
                );
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

    }
}
