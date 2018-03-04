﻿using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain
{
    public enum CashoutState
    {
        Started,
        OperationIsFinished,
        [Obsolete("Should be removed with next release")]
        ClientOperationFinishIsRegistered,
        //StartedCrossClient,
        //EnrolledToMatchingEngine
    }
}
