using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Core.Repositories;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.CrossClient;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers.CrossClient
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
            ILogFactory logFactory,
            IBlockchainWalletsClient walletsClient,
            IMatchingEngineCallsDeduplicationRepository deduplicationRepository,
            IMatchingEngineClient meClient)
        {
            _chaosKitty = chaosKitty ?? throw new ArgumentNullException(nameof(chaosKitty));
            _log = logFactory.CreateLog(this);
            _deduplicationRepository = deduplicationRepository ?? throw new ArgumentNullException(nameof(deduplicationRepository));
            _meClient = meClient ?? throw new ArgumentNullException(nameof(meClient));
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(EnrollToMatchingEngineCommand command, IEventPublisher publisher)
        {
            // First level deduplication just to reduce traffic to the ME
            if (await _deduplicationRepository.IsExistsAsync(command.CashinOperationId))
            {
                _log.Info("Deduplicated at first level", command);

                // Workflow should be continued

                publisher.PublishEvent(new CashinEnrolledToMatchingEngineEvent
                {
                    CashoutOperationId = command.CashoutOperationId
                });

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

            switch (cashInResult.Status)
            {
                case MeStatusCodes.Ok:
                case MeStatusCodes.Duplicate:
                    if (cashInResult.Status == MeStatusCodes.Duplicate)
                    {
                        _log.Info("Deduplicated by the ME", command);
                    }

                    publisher.PublishEvent(new CashinEnrolledToMatchingEngineEvent
                    {
                        CashoutOperationId = command.CashoutOperationId
                    });

                    _chaosKitty.Meow(command.CashinOperationId);

                    await _deduplicationRepository.InsertOrReplaceAsync(command.CashinOperationId);

                    _chaosKitty.Meow(command.CashinOperationId);

                    return CommandHandlingResult.Ok();

                case MeStatusCodes.Runtime:
                    // Retry forever with the default delay + log the error outside.
                    throw new Exception($"Cashin into the ME is failed. ME status: {cashInResult.Status}, ME message: {cashInResult.Message}");

                default:
                    // Just abort cashout for further manual processing. ME call could not be retried anyway if responce was received.
                    _log.Warning(
                        $"Unexpected response from ME. Status: {cashInResult.Status}, ME message: {cashInResult.Message}",
                        context: command);
                    return CommandHandlingResult.Ok();
            }
        }
    }
}
