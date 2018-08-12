using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public class BatchedCashoutValueType : IEquatable<BatchedCashoutValueType>
    {
        public Guid OperationId { get; }
        public string DestinationAddress { get; }
        public decimal Amount { get; }

        public BatchedCashoutValueType(Guid operationId, string destinationAddress, decimal amount)
        {
            OperationId = operationId;
            DestinationAddress = destinationAddress;
            Amount = amount;
        }

        public bool Equals(BatchedCashoutValueType other)
        {
            if (ReferenceEquals(null, other)) 
                return false;
            if (ReferenceEquals(this, other)) 
                return true;

            return OperationId.Equals(other.OperationId) && string.Equals(DestinationAddress, other.DestinationAddress) && Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) 
                return false;
            if (ReferenceEquals(this, obj)) 
                return true;
            if (obj.GetType() != this.GetType()) 
                return false;

            return Equals((BatchedCashoutValueType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OperationId.GetHashCode();
                hashCode = (hashCode * 397) ^ (DestinationAddress != null ? DestinationAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Amount.GetHashCode();

                return hashCode;
            }
        }
    }
}
