using System;

namespace Lykke.Job.BlockchainCashoutProcessor.Core.Domain.Batching
{
    public class BatchedCashoutValueType : IEquatable<BatchedCashoutValueType>
    {
        public Guid CashoutId { get; }
        public Guid ClientId { get; }
        public string ToAddress { get; }
        public decimal Amount { get; }

        public BatchedCashoutValueType(Guid cashoutId, Guid clientId, string toAddress, decimal amount)
        {
            CashoutId = cashoutId;
            ClientId = clientId;
            ToAddress = toAddress;
            Amount = amount;
        }

        public bool Equals(BatchedCashoutValueType other)
        {
            if (ReferenceEquals(null, other)) 
                return false;
            if (ReferenceEquals(this, other)) 
                return true;

            return CashoutId.Equals(other.CashoutId) 
                   && ClientId.Equals(other.ClientId)
                   && string.Equals(ToAddress, other.ToAddress) 
                   && Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) 
                return false;
            if (ReferenceEquals(this, obj)) 
                return true;
            if (obj.GetType() != GetType()) 
                return false;

            return Equals((BatchedCashoutValueType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CashoutId.GetHashCode();
                hashCode = (hashCode * 397) ^ ClientId.GetHashCode();
                hashCode = (hashCode * 397) ^ (ToAddress != null ? ToAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Amount.GetHashCode();

                return hashCode;
            }
        }
    }
}
