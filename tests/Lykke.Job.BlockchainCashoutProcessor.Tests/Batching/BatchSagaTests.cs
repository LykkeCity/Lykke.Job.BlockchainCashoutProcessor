using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Commands;
using Moq;
using Xunit;

namespace Lykke.Job.BlockchainCashoutProcessor.Tests.Batching
{
    public class BatchSagaTests
    {
        public BatchSagaTests()
        {
        }

        [Fact]
        public async Task Batch_ActiveBatchIdRevokedEvent_Outputs_Aggregated()
        {
            Mock<IChaosKitty> chaosKittyMock = new Mock<IChaosKitty>();
            Mock<ICashoutsBatchReadOnlyRepository> cashoutsBatchReadOnlyRepository =
                new Mock<ICashoutsBatchReadOnlyRepository>();
            Mock<ICommandSender> commandSender =
                new Mock<ICommandSender>();
            Guid batchId = Guid.NewGuid();
            var cashoutAggregate = CashoutsBatchAggregate.Start(
                batchId,
                "Icon",
                "ICX",
                "ICX",
                "hx...",
                21,
                TimeSpan.FromDays(1));

            cashoutAggregate.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), "hx1...", 1, 1, DateTime.UtcNow));
            cashoutAggregate.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), "hx1...", 2, 1, DateTime.UtcNow));
            cashoutAggregate.AddCashout(new BatchedCashoutValueType(Guid.NewGuid(), Guid.NewGuid(), "hx2...", 2, 1, DateTime.UtcNow));

            cashoutsBatchReadOnlyRepository.Setup(x => x.GetAsync(batchId)).ReturnsAsync(cashoutAggregate);
            var batchSaga = new BatchSaga(chaosKittyMock.Object, cashoutsBatchReadOnlyRepository.Object);

            ActiveBatchIdRevokedEvent @event = new ActiveBatchIdRevokedEvent()
            {
                BatchId = batchId
            };

            await batchSaga.Handle(@event, commandSender.Object);
            commandSender.Verify(x =>
                x.SendCommand<StartOneToManyOutputsExecutionCommand>(
                    It.Is<StartOneToManyOutputsExecutionCommand>(y => CheckStartOneToManyOutputsExecutionCommand(y)),
                    It.IsAny<string>(),
                    It.IsAny<uint>()));
        }

        private bool CheckStartOneToManyOutputsExecutionCommand(StartOneToManyOutputsExecutionCommand command)
        {
            return command.Outputs.Length == 2 && command.Outputs.FirstOrDefault().Amount == 3;
        }
    }
}
