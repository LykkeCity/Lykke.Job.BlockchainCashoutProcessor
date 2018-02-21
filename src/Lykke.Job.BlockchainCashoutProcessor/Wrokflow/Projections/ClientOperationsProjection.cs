using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Projections
{
    public class ClientOperationsProjection
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly ICashoutRepository _cashoutRepository;
        private readonly ICashOperationsRepositoryClient _clientOperationsRepositoryClient;

        public ClientOperationsProjection(
            IChaosKitty chaosKitty, 
            ILog log, 
            ICashoutRepository cashoutRepository,
            ICashOperationsRepositoryClient clientOperationsRepositoryClient)
        {
            _chaosKitty = chaosKitty;
            _log = log;
            _cashoutRepository = cashoutRepository;
            _clientOperationsRepositoryClient = clientOperationsRepositoryClient;
        }

        [UsedImplicitly]
        public async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt)
        {
            _log.WriteInfo($"{nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent)} Projection", evt, "");
            try
            {

                var aggregate = await _cashoutRepository.TryGetAsync(evt.OperationId);

                if (aggregate == null)
                {
                    // This is not a cashout operation
                    return;
                }

                await _clientOperationsRepositoryClient.UpdateBlockchainHashAsync(
                    aggregate.ClientId.ToString(),
                    aggregate.OperationId.ToString(),
                    evt.TransactionHash);

                _chaosKitty.Meow(evt.OperationId);
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, ex);
                throw;
            }
        }
    }
}
