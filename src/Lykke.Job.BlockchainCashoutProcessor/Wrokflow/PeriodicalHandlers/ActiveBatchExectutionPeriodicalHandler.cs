using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services;
using Lykke.Job.BlockchainCashoutProcessor.Core.Services.Blockchains;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Commands;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.PeriodicalHandlers
{
    [UsedImplicitly]
    public class ActiveBatchExectutionPeriodicalHandler: IActiveBatchExectutionPeriodicalHandler
    {
        private readonly ITimerTrigger _timer;
        private readonly string _blockchainType;
        private readonly IActiveBatchRepository _activeBatchRepository;
        private readonly BlockchainCashoutAggregationConfiguration _aggregationConfiguration;
        private readonly ICqrsEngine _cqrsEngine;

        public ActiveBatchExectutionPeriodicalHandler(ILog log, 
            string blockchainType,
            TimeSpan period, 
            IActiveBatchRepository activeBatchRepository,
            BlockchainCashoutAggregationConfiguration aggregationConfiguration, 
            ICqrsEngine cqrsEngine)
        {
            _blockchainType = blockchainType;
            _activeBatchRepository = activeBatchRepository;
            _aggregationConfiguration = aggregationConfiguration;
            _cqrsEngine = cqrsEngine;

            _timer = new TimerTrigger(
                $"{nameof(ActiveBatchExectutionPeriodicalHandler)} : {blockchainType}",
                period,
                log);

            _timer.Triggered += StartActiveBatchExectionIfNeeded;
        }

        private async Task StartActiveBatchExectionIfNeeded(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken cancellationToken)
        {
            var activeBatches = await _activeBatchRepository.GetAsync(_blockchainType);

            foreach (var batch in activeBatches.Where(p => p.NeedToStartBatchExecution(_aggregationConfiguration)))
            {
                _cqrsEngine.SendCommand
                (
                    new StartBatchExecutionCommand
                    {
                        BatchId = batch.BatchId,
                        StartedAt = batch.StartedAt,
                        BlockchainType = batch.BlockchainType,
                        BlockchainAssetId = batch.BlockchainAssetId,
                        HotWallet = batch.HotWallet
                    },
                    BlockchainCashoutProcessorBoundedContext.Name,
                    BlockchainCashoutProcessorBoundedContext.Name
                );
            }
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
