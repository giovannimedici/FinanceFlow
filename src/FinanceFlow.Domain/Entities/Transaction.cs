using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Domain.Entities;

public sealed class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public Guid? RelatedAccountId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? Description { get; private set; }

    private Transaction() { }  

    public static Transaction Create(
        Guid accountId,
        TransactionType type,
        decimal amount,
        decimal balanceAfter,
        Guid? relatedAccountId = null,
        string? description = null)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Type = type,
            Amount = amount,
            BalanceAfter = balanceAfter,
            RelatedAccountId = relatedAccountId,
            CreatedAt = DateTimeOffset.UtcNow,
            Description = description
        };
    }
}