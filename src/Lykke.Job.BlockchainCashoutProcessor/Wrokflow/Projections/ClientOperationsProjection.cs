using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Projections
{
    public class ClientOperationsProjection
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly ICashoutRepository _cashoutRepository;
        private readonly ICrossClientCashoutRepository _crossClientCashoutRepository;
        private readonly ICashOperationsRepositoryClient _clientOperationsRepositoryClient;

        public ClientOperationsProjection(
            IChaosKitty chaosKitty, 
            ILog log, 
            ICashoutRepository cashoutRepository,
            ICrossClientCashoutRepository crossClientCashoutRepository,
            ICashOperationsRepositoryClient clientOperationsRepositoryClient)
        {
            _chaosKitty = chaosKitty;
            _log = log.CreateComponentScope(nameof(ClientOperationsProjection));
            _cashoutRepository = cashoutRepository;
            _crossClientCashoutRepository = crossClientCashoutRepository;
            _clientOperationsRepositoryClient = clientOperationsRepositoryClient;
        }

        [UsedImplicitly]
        public async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt)
        {
            _log.WriteInfo(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, "");

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

        [UsedImplicitly]
        public async Task Handle(CashinEnrolledToMatchingEngineEvent evt)
        {
            _log.WriteInfo(nameof(CashinEnrolledToMatchingEngineEvent), evt, "");

            var aggregate = await _crossClientCashoutRepository.GetAsync(evt.CashoutOperationId);

            await _clientOperationsRepositoryClient.RegisterAsync(new CashInOutOperation(
                id: aggregate.CashinOperationId.ToString(),
                transactionId: aggregate.CashinOperationId.ToString(),
                dateTime: aggregate.StartMoment,
                amount: (double)aggregate.Amount,
                assetId: aggregate.AssetId,
                clientId: aggregate.RecipientClientId.ToString(),
                addressFrom: aggregate.ToAddress,
                addressTo: aggregate.HotWalletAddress,
                type: CashOperationType.ForwardCashIn,
                state: TransactionStates.SettledNoChain,
                isSettled: false,
                blockChainHash: "",

                // These fields are not used

                feeType: FeeType.Unknown,
                feeSize: 0,
                isRefund: false,
                multisig: "",
                isHidden: false
            ));

            _chaosKitty.Meow(evt.CashoutOperationId);
        }
    }
}
