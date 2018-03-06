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
using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class EnrollToMatchingEngineCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;
        private readonly IMatchingEngineClient _meClient;

        public EnrollToMatchingEngineCommandsHandler(
            IChaosKitty chaosKitty,
            ILog log,
            IBlockchainWalletsClient walletsClient,
            IMatchingEngineCallsDeduplicationRepository deduplicationRepository,
            IMatchingEngineClient meClient)
        {
            _chaosKitty = chaosKitty;
            _log = log;
            _deduplicationRepository = deduplicationRepository;
            _meClient = meClient;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(EnrollToMatchingEngineCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(EnrollToMatchingEngineCommand), command, "");

            // First level deduplication just to reduce traffic to the ME
            if (await _deduplicationRepository.IsExistsAsync(command.CashinOperationId))
            {
                _log.WriteInfo(nameof(EnrollToMatchingEngineCommand), command.CashinOperationId, "Deduplicated");

                // Just skips
                return CommandHandlingResult.Ok();
            }

            var cashInResult = await _meClient.CashInOutAsync(
                command.CashinOperationId.ToString(),
                command.RecipientClientId.ToString(),
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
                    CashoutOperationId = command.CashoutOperationId
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
