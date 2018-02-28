using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Repositories
{
    public interface IMatchingEngineCallsDeduplicationRepository
    {
        Task InsertOrReplaceAsync(Guid operationId);
        Task<bool> IsExistsAsync(Guid operationId);
        Task TryRemoveAsync(Guid operationId);
    }
}
