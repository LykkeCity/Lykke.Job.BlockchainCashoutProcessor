using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Repositories;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class EnrollToMatchingEngineCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly IBlockchainWalletsClient _walletsClient;
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;
        private readonly IMatchingEngineClient _meClient;
        private readonly ICashOperationsRepositoryClient _cashOperationsRepositoryClient;

        public EnrollToMatchingEngineCommandsHandler(
            IChaosKitty chaosKitty,
            ILog log,
            IBlockchainWalletsClient walletsClient,
            IMatchingEngineCallsDeduplicationRepository deduplicationRepository,
            IMatchingEngineClient meClient,
            ICashOperationsRepositoryClient cashOperationsRepositoryClient)
        {
            _chaosKitty = chaosKitty;
            _log = log;
            _walletsClient = walletsClient;
            _deduplicationRepository = deduplicationRepository;
            _meClient = meClient;
            _cashOperationsRepositoryClient = cashOperationsRepositoryClient;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(EnrollToMatchingEngineCommand command, IEventPublisher publisher)
        {

            _log.WriteInfo(nameof(EnrollToMatchingEngineCommand), command, "");

            // First level deduplication just to reduce traffic to the ME
            if (await _deduplicationRepository.IsExistsAsync(command.CashinOperationId))
            {
                _log.WriteInfo(nameof(EnrollToMatchingEngineCommand), command.CashoutOperationId, "Deduplicated");

                // Just skips
                return CommandHandlingResult.Ok();
            }

            // TODO: Add client cache for the walletsClient

            var clientId = await _walletsClient.TryGetClientIdAsync(
                command.BlockchainType,
                command.BlockchainAssetId,
                command.DepositWalletAddress);

            if (clientId == null)
            {
                throw new InvalidOperationException("Client ID for the blockchain deposit wallet address is not found");
            }

            var cashInResult = await _meClient.CashInOutAsync(
                command.CashinOperationId.ToString(),
                clientId.Value.ToString(),
                command.AssetId,
                (double)command.Amount);

            _chaosKitty.Meow(command.CashoutOperationId);

            if (cashInResult == null)
            {
                throw new InvalidOperationException("ME response is null, don't know what to do");
            }

            if (cashInResult.Status == MeStatusCodes.Ok ||
                cashInResult.Status == MeStatusCodes.Duplicate)
            {
                if (cashInResult.Status == MeStatusCodes.Duplicate)
                {
                    _log.WriteInfo(nameof(EnrollToMatchingEngineCommand), command.CashoutOperationId, "Deduplicated by the ME");
                }

                publisher.PublishEvent(new CashinEnrolledToMatchingEngineEvent
                {
                    CashoutOperationId = command.CashoutOperationId,
                    CashinOperationId = command.CashinOperationId,
                    ClientId = clientId.Value
                });

                _chaosKitty.Meow(command.CashinOperationId);

                await _deduplicationRepository.InsertOrReplaceAsync(command.CashinOperationId);

                _chaosKitty.Meow(command.CashinOperationId);

                return CommandHandlingResult.Ok();
            }

            throw new InvalidOperationException($"Cashin into the ME is failed. ME status: {cashInResult.Status}, ME message: {cashInResult.Message}");
        }
    }
}
