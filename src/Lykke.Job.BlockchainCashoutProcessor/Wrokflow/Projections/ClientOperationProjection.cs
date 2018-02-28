﻿using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashoutProcessor.Core.Domain;
using Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Events;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Wrokflow.Projections
{
    public class ClientOperationsProjection
    {
        private readonly ILog _log;
        private readonly ICashoutRepository _cashoutRepository;
        private readonly IBlockchainWalletsClient _walletsClient;
        private readonly ICashOperationsRepositoryClient _clientOperationsRepositoryClient;
        private readonly IChaosKitty _chaosKitty;

        public ClientOperationsProjection(
            ILog log,
            ICashoutRepository cashoutRepository,
            IBlockchainWalletsClient walletsClient,
            ICashOperationsRepositoryClient clientOperationsRepositoryClient,
            IChaosKitty chaosKitty)
        {
            _log = log.CreateComponentScope(nameof(ClientOperationsProjection));
            _cashoutRepository = cashoutRepository;
            _walletsClient = walletsClient;
            _clientOperationsRepositoryClient = clientOperationsRepositoryClient;
            _chaosKitty = chaosKitty;
        }

        [UsedImplicitly]
        public async Task Handle(CashinEnrolledToMatchingEngineEvent evt)
        {
            _log.WriteInfo(nameof(CashinEnrolledToMatchingEngineEvent), evt, "");

            try
            {
                var aggregate = await _cashoutRepository.GetAsync(evt.CashoutOperationId);
                var clientId = await _walletsClient.TryGetClientIdAsync(
                    aggregate.BlockchainType,
                    aggregate.BlockchainAssetId,
                    aggregate.ToAddress);

                if (clientId == null)
                {
                    throw new InvalidOperationException("Client ID for the blockchain deposit wallet address is not found");
                }

                await _clientOperationsRepositoryClient.RegisterAsync(new CashInOutOperation(
                    id: aggregate.CrossClientOperationId.ToString(),
                    transactionId: aggregate.CrossClientOperationId.ToString(),
                    dateTime: aggregate.StartMoment,
                    amount: (double)aggregate.Amount,
                    assetId: aggregate.AssetId,
                    clientId: clientId.ToString(),
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
            catch (Exception ex)
            {
                _log.WriteError(nameof(CashinEnrolledToMatchingEngineEvent), evt, ex);
                throw;
            }
        }

        //[UsedImplicitly]
        //public async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt)
        //{
        //    _log.WriteInfo(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, "");

        //    try
        //    {
        //        var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

        //        // Obtains clientId directly from the wallets, but not aggregate,
        //        // to make projection independent on the aggregate state, since
        //        // clientId in aggregate is initially not filled up.

        //        // TODO: Add client cache for the walletsClient

        //        var clientId = await _walletsClient.TryGetClientIdAsync(
        //            aggregate.BlockchainType,
        //            aggregate.BlockchainAssetId,
        //            aggregate.DepositWalletAddress);

        //        if (clientId == null)
        //        {
        //            throw new InvalidOperationException(
        //                "Client ID for the blockchain deposit wallet address is not found");
        //        }

        //        await _clientOperationsRepositoryClient.UpdateBlockchainHashAsync(
        //            clientId.ToString(),
        //            evt.OperationId.ToString(),
        //            evt.TransactionHash);

        //        _chaosKitty.Meow(evt.OperationId);
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.WriteError(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, ex);
        //        throw;
        //    }
        //}
    }
}

