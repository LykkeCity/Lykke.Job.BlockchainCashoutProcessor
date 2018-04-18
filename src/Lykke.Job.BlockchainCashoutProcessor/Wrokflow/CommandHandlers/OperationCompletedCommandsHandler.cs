using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.CommandHandlers
{
    [UsedImplicitly]
    public class OperationCompletedCommandsHandler
    {
        private readonly ILog _log;

        public OperationCompletedCommandsHandler(
            ILog log)
        {
            _log = log;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(NotifyCashoutCompletedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent(new CashoutCompletedEvent()
            {
                ToAddress = command.ToAddress,
                AssetId = command.AssetId,
                ClientId = command.ClientId,
                Amount = command.Amount,
                TransactionHash = command.TransactionHash
            });

            return CommandHandlingResult.Ok();
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(NotifyCashinCompletedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent(new CashinCompletedEvent()
            {
                ClientId = command.ClientId ,
                AssetId =  command.AssetId,
                Amount = command.Amount
            });

            return CommandHandlingResult.Ok();
        }
    }
}
