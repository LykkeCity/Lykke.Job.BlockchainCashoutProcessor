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
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands.Regular;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Sagas
{
    /// <summary>
    /// -> Lykke.Service.Operations : StartCashoutCommand
    /// -> CashoutStartedEvent
    ///     -> BlockchainRiskControl : ValidateOperationCommand
    /// -> BlockchainRiskControl : OperationAcceptedEvent | OperationRejectedEvent
    /// </summary>
    [UsedImplicitly]
    public class RiskControlSaga
    {
        private static readonly string Self = BlockchainCashoutProcessorBoundedContext.Name;

        private readonly IChaosKitty _chaosKitty;
        private readonly ICashoutRiskControlRepository _riskControlRepository;

        public RiskControlSaga(
            IChaosKitty chaosKitty,
            ICashoutRiskControlRepository riskControlRepository)
        {
            _chaosKitty = chaosKitty;
            _riskControlRepository = riskControlRepository;
        }

        [UsedImplicitly]
        public async Task Handle(ValidationStartedEvent evt, ICommandSender sender)
        {
            var aggregate = await _riskControlRepository.GetOrAddAsync(
                evt.OperationId,
                () => CashoutRiskControlAggregate.Create(
                    evt.OperationId,
                    evt.ClientId,
                    evt.AssetId,
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
        public async Task Handle(OperationAcceptedEvent evt, ICommandSender sender)
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
                        ClientId = aggregate.ClientId,
                        AssetId = aggregate.AssetId,
                        BlockchainType = aggregate.BlockchainType,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        HotWalletAddress = aggregate.HotWalletAddress,
                        ToAddress = aggregate.ToAddress,
                        Amount = aggregate.Amount
                    },
                    Self
                );

                _chaosKitty.Meow(evt.OperationId);

                await _riskControlRepository.SaveAsync(aggregate);

                _chaosKitty.Meow(evt.OperationId);
            }
        }

        [UsedImplicitly]
        public async Task Handle(OperationRejectedEvent evt, ICommandSender sender)
        {
            var aggregate = await _riskControlRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                return; // not a cashout operation
            }

            if (aggregate.OnOperationRejected(evt.Message))
            {
                // just send a regular CashoutFailed notification
                sender.SendCommand
                (
                    new NotifyCashoutFailedCommand
                    {
                        OperationId = aggregate.OperationId,
                        ClientId = aggregate.ClientId,
                        AssetId = aggregate.AssetId,
                        Amount = aggregate.Amount,
                        StartMoment = aggregate.StartMoment.Value,
                        FinishMoment = aggregate.OperationRejectionMoment.Value,
                        Error = aggregate.Error,
                        ErrorCode = CashoutErrorCode.Unknown
                    },
                    CqrsModule.Self
                );

                _chaosKitty.Meow(evt.OperationId);

                await _riskControlRepository.SaveAsync(aggregate);

                _chaosKitty.Meow(evt.OperationId);
            }
        }
    }
}
