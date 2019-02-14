using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.RiskControl;
using Lykke.Job.BlockchainCashoutProcessor.Modules;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.CrossClient;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events.RiskControl;
using Lykke.Job.BlockchainRiskControl.Contract;
using Lykke.Job.BlockchainRiskControl.Contract.Commands;
using Lykke.Job.BlockchainRiskControl.Contract.Events;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.RiskControl;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas
{
    /// <summary>
    /// 
    /// </summary>
    [UsedImplicitly]
    public class RiskControlSaga
    {
        private static readonly string Self = BlockchainCashoutProcessorBoundedContext.Name;

        private readonly IChaosKitty _chaosKitty;
        private readonly IRiskControlRepository _riskControlRepository;

        public RiskControlSaga(
            IChaosKitty chaosKitty,
            IRiskControlRepository riskControlRepository)
        {
            _chaosKitty = chaosKitty;
            _riskControlRepository = riskControlRepository;
        }

        [UsedImplicitly]
        private async Task Handle(ValidationStartedEvent evt, ICommandSender sender)
        {
            var aggregate = await _riskControlRepository.GetOrAddAsync(
                evt.OperationId,
                () => RiskControlAggregate.Create(
                    evt.OperationId,
                    evt.ClientId,
                    evt.BlockchainType,
                    evt.BlockchainAssetId,
                    evt.HotWalletAddress,
                    evt.ToAddress,
                    evt.Amount));

            _chaosKitty.Meow(evt.OperationId);

            if (aggregate.Start())
            {
                sender.SendCommand
                (
                    new ValidateOperationCommand
                    {
                        OperationId = evt.OperationId,
                        UserId = evt.ClientId,
                        Type = OperationType.Withdrawal,
                        BlockchainAssetId = evt.BlockchainAssetId,
                        BlockchainType = evt.BlockchainType,
                        FromAddress = evt.HotWalletAddress,
                        ToAddress = evt.ToAddress,
                        Amount = evt.Amount
                    },
                    BlockchainRiskControlBoundedContext.Name
                );

                _chaosKitty.Meow(evt.OperationId);

                await _riskControlRepository.SaveAsync(aggregate);

                _chaosKitty.Meow(evt.OperationId);
            }
        }

        [UsedImplicitly]
        private async Task Handle(OperationAcceptedEvent evt, ICommandSender sender)
        {
            var aggregate = await _riskControlRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                return; // not a cashout operation
            }

            if (aggregate.OnOperationAccepted())
            {
                sender.SendCommand
                (
                    new AcceptCashoutCommand
                    {
                        OperationId = evt.OperationId,
                        UserId = evt.ClientId,
                        Type = OperationType.Withdrawal,
                        BlockchainAssetId = evt.BlockchainAssetId,
                        BlockchainType = evt.BlockchainType,
                        HotWalletAddress = evt.FromAddress,
                        ToAddress = evt.ToAddress,
                        Amount = evt.Amount
                    },
                    Self
                );

                _chaosKitty.Meow(evt.OperationId);

                await _riskControlRepository.SaveAsync(aggregate);

                _chaosKitty.Meow(evt.OperationId);
            }
        }

        [UsedImplicitly]
        private async Task Handle(OperationRejectedEvent evt, ICommandSender sender)
        {
            var aggregate = await _riskControlRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                return; // not a cashout operation
            }

            if (aggregate.OnOperationRejected(evt.Message))
            {
                sender.SendCommand
                (
                    new RejectCashoutCommand
                    {
                        OperationId = evt.OperationId,
                        UserId = evt.ClientId,
                        Type = OperationType.Withdrawal,
                        BlockchainAssetId = evt.BlockchainAssetId,
                        BlockchainType = evt.BlockchainType,
                        FromAddress = evt.FromAddress,
                        ToAddress = evt.ToAddress,
                        Amount = evt.Amount
                    },
                    Self
                );

                _chaosKitty.Meow(evt.OperationId);

                await _riskControlRepository.SaveAsync(aggregate);

                _chaosKitty.Meow(evt.OperationId);
            }
        }
    }
}
