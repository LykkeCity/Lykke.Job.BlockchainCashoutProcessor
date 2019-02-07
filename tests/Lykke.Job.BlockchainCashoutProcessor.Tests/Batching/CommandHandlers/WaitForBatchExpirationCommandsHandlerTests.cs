using System;
using System.Threading.Tasks;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.AzureRepositories.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;
using Moq;
using Xunit;

namespace Lykke.Job.BlockchainCashoutProcessor.Tests.Batching.CommandHandlers
{
    public class WaitForBatchExpirationCommandsHandlerTests
    {
        private readonly TimeSpan _expirationMonitoringPeriod;
        private readonly Mock<IEventPublisher> _eventsPublisherMock;
        private readonly Mock<ICashoutsBatchRepository> _batchRepositoryMock;
        private readonly WaitForBatchExpirationCommandsHandler _handler;

        private CashoutsBatchAggregate _batch;

        public WaitForBatchExpirationCommandsHandlerTests()
        {
            _expirationMonitoringPeriod = TimeSpan.FromMinutes(2);

            _eventsPublisherMock = new Mock<IEventPublisher>();
            _batchRepositoryMock = new Mock<ICashoutsBatchRepository>();

            _handler = new WaitForBatchExpirationCommandsHandler
            (
                new SilentChaosKitty(),
                _expirationMonitoringPeriod,
                _batchRepositoryMock.Object
            );

            _batch = CashoutsBatchAggregate.Start
            (
                CashoutsBatchAggregate.GetNextId(),
                "Bitcoin",
                "LykkeBTC",
                "BTC",
                "HotWallet",
                10,
                TimeSpan.FromMinutes(10)
            );
            
            _batchRepositoryMock
                .Setup(x => x.GetAsync(It.Is<Guid>(p => p == _batch.BatchId)))
                .ReturnsAsync(() => _batch);
        }

        [Fact]
        public async Task Continue_Waiting_If_Batch_Is_Not_Expired()
        {
            // Arrange

            _batch.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), "Destination", 100));
            
            // Act

            var handlingResult = await _handler.Handle
            (
                new WaitForBatchExpirationCommand
                {
                    BatchId = _batch.BatchId
                },
                _eventsPublisherMock.Object
            );

            // Assert
            
            Assert.True(handlingResult.Retry);
            Assert.Equal(_expirationMonitoringPeriod, TimeSpan.FromMilliseconds(handlingResult.RetryDelay));
            Assert.Equal(CashoutsBatchState.FillingUp, _batch.State);
            
            _batchRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CashoutsBatchAggregate>()), Times.Never);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.IsAny<BatchExpiredEvent>()), Times.Never);
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Abort_Waiting_If_Batch_Is_Already_Filled()
        {
            // Arrange

            for (var i = 0; i < _batch.CountThreshold; ++i)
            {
                _batch.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), $"Destination-{i}", 100));
            }
            
            // Act

            var handlingResult = await _handler.Handle
            (
                new WaitForBatchExpirationCommand
                {
                    BatchId = _batch.BatchId
                },
                _eventsPublisherMock.Object
            );

            // Assert
            
            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.Filled, _batch.State);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CashoutsBatchAggregate>()), Times.Never);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.IsAny<BatchExpiredEvent>()), Times.Never);
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Expire_Batch_If_Batch_Is_Have_To_Be_Expired()
        {
            // Arrange

            _batch.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), "Destination", 100));

            var batchEntity = CashoutsBatchEntity.FromDomain(_batch);

            batchEntity.StartMoment = DateTime.UtcNow - _batch.AgeThreshold - TimeSpan.FromSeconds(1);

            _batch = batchEntity.ToDomain();
            
            // Act

            var handlingResult = await _handler.Handle
            (
                new WaitForBatchExpirationCommand
                {
                    BatchId = _batch.BatchId
                },
                _eventsPublisherMock.Object
            );

            // Assert
            
            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.Expired, _batch.State);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.Is<BatchExpiredEvent>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Expire_Batch_If_Batch_Is_Have_To_Be_Expired_With_Batch_Saving_Failure()
        {
            // == Step 1 - Failure ==
             
            // Arrange 1

            _batch.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), "Destination", 100));

            var batchEntity = CashoutsBatchEntity.FromDomain(_batch);

            batchEntity.StartMoment = DateTime.UtcNow - _batch.AgeThreshold - TimeSpan.FromSeconds(1);

            _batch = batchEntity.ToDomain();

            _batchRepositoryMock
                .Setup(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)))
                .Throws<InvalidOperationException>();
            
            // Act/Assert 1

            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle
            (
                new WaitForBatchExpirationCommand
                {
                    BatchId = _batch.BatchId
                },
                _eventsPublisherMock.Object
            ));

            // Assert 1

            Assert.Equal(CashoutsBatchState.Expired, _batch.State);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.IsAny<BatchExpiredEvent>()), Times.Never);
            _eventsPublisherMock.VerifyNoOtherCalls();

            // == Step 2 - Success ==

            // Arrange 2

            _batchRepositoryMock.Invocations.Clear();
            _eventsPublisherMock.Invocations.Clear();

            _batchRepositoryMock
                .Setup(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)))
                .Returns(Task.CompletedTask);

            // Act 2

            var handlingResult = await _handler.Handle
            (
                new WaitForBatchExpirationCommand
                {
                    BatchId = _batch.BatchId
                },
                _eventsPublisherMock.Object
            );

            // Assert 2
            
            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.Expired, _batch.State);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CashoutsBatchAggregate>()), Times.Never);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.Is<BatchExpiredEvent>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Expire_Batch_If_Batch_Is_Have_To_Be_Expired_With_Event_Publishing_Failure()
        {
            // == Step 1 - Failure ==
             
            // Arrange 1

            _batch.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), "Destination", 100));

            var batchEntity = CashoutsBatchEntity.FromDomain(_batch);

            batchEntity.StartMoment = DateTime.UtcNow - _batch.AgeThreshold - TimeSpan.FromSeconds(1);

            _batch = batchEntity.ToDomain();

            _eventsPublisherMock
                .Setup(x => x.PublishEvent(It.Is<BatchExpiredEvent>(p => p.BatchId == _batch.BatchId)))
                .Throws<InvalidOperationException>();
            
            // Act/Assert 1

            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle
            (
                new WaitForBatchExpirationCommand
                {
                    BatchId = _batch.BatchId
                },
                _eventsPublisherMock.Object
            ));

            // Assert 1

            Assert.Equal(CashoutsBatchState.Expired, _batch.State);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.Is<BatchExpiredEvent>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.VerifyNoOtherCalls();

            // == Step 2 - Success ==

            // Arrange 2

            _batchRepositoryMock.Invocations.Clear();
            _eventsPublisherMock.Invocations.Clear();
            
            _eventsPublisherMock
                .Setup(x => x.PublishEvent(It.Is<BatchExpiredEvent>(p => p.BatchId == _batch.BatchId)))
                .Callback(() => { });

            // Act 2

            var handlingResult = await _handler.Handle
            (
                new WaitForBatchExpirationCommand
                {
                    BatchId = _batch.BatchId
                },
                _eventsPublisherMock.Object
            );

            // Assert 2
            
            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.Expired, _batch.State);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CashoutsBatchAggregate>()), Times.Never);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.Is<BatchExpiredEvent>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Expire_Batch_If_Batch_Is_Have_To_Be_Expired_With_Batch_Saving_And_Event_Publishing_Failures()
        {
            // == Step 1 - Batch saving failure ==

            // Arrange 1

            _batch.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), "Destination", 100));

            var batchEntity = CashoutsBatchEntity.FromDomain(_batch);

            batchEntity.StartMoment = DateTime.UtcNow - _batch.AgeThreshold - TimeSpan.FromSeconds(1);

            _batch = batchEntity.ToDomain();

            _batchRepositoryMock
                .Setup(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)))
                .Throws<InvalidOperationException>();
            
            // Act/Assert 1

            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle
            (
                new WaitForBatchExpirationCommand
                {
                    BatchId = _batch.BatchId
                },
                _eventsPublisherMock.Object
            ));

            // Assert 1

            Assert.Equal(CashoutsBatchState.Expired, _batch.State);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.IsAny<BatchExpiredEvent>()), Times.Never);
            _eventsPublisherMock.VerifyNoOtherCalls();

            // == Step 2 - Event publishing failure ==
             
            _batchRepositoryMock.Invocations.Clear();
            _eventsPublisherMock.Invocations.Clear();

            _batchRepositoryMock
                .Setup(x => x.SaveAsync(It.Is<CashoutsBatchAggregate>(p => p.BatchId == _batch.BatchId)))
                .Returns(Task.CompletedTask);

            _eventsPublisherMock
                .Setup(x => x.PublishEvent(It.Is<BatchExpiredEvent>(p => p.BatchId == _batch.BatchId)))
                .Throws<InvalidOperationException>();
            
            // Act/Assert 2

            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle
            (
                new WaitForBatchExpirationCommand
                {
                    BatchId = _batch.BatchId
                },
                _eventsPublisherMock.Object
            ));

            // Assert 2

            Assert.Equal(CashoutsBatchState.Expired, _batch.State);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CashoutsBatchAggregate>()), Times.Never);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.Is<BatchExpiredEvent>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.VerifyNoOtherCalls();

            // == Step 3 - Success ==

            // Arrange 3

            _batchRepositoryMock.Invocations.Clear();
            _eventsPublisherMock.Invocations.Clear();
            
            _eventsPublisherMock
                .Setup(x => x.PublishEvent(It.Is<BatchExpiredEvent>(p => p.BatchId == _batch.BatchId)))
                .Callback(() => { });

            // Act 3

            var handlingResult = await _handler.Handle
            (
                new WaitForBatchExpirationCommand
                {
                    BatchId = _batch.BatchId
                },
                _eventsPublisherMock.Object
            );

            // Assert 3
            
            Assert.False(handlingResult.Retry);
            Assert.Equal(CashoutsBatchState.Expired, _batch.State);

            _batchRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<CashoutsBatchAggregate>()), Times.Never);
            _eventsPublisherMock.Verify(x => x.PublishEvent(It.Is<BatchExpiredEvent>(p => p.BatchId == _batch.BatchId)), Times.Once);
            _eventsPublisherMock.VerifyNoOtherCalls();
        }
    }
}
