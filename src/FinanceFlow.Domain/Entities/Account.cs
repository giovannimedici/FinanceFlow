using FinanceFlow.Domain.Exceptions;
using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; private set; }
    public string OwnerName { get; private set; } = default!;
    public string DocumentNumber { get; private set; } = default!;
    public decimal Balance { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Account() { }

    public static Account Create(string ownerName, string documentNumber)
    {
        if (string.IsNullOrWhiteSpace(ownerName))
            throw new DomainException("Owner name is required");

        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new DomainException("Document number is required");

        var now = DateTimeOffset.UtcNow;

        return new Account
        {
            Id = Guid.NewGuid(),
            OwnerName = ownerName,
            DocumentNumber = documentNumber,
            Balance = 0m,
            Status = AccountStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Deposit amount must be greater than zero");

        if (Status != AccountStatus.Active)
            throw new DomainException($"Cannot deposit into an account with status: {Status}");

        Balance += amount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Withdrawal amount must be greater than zero");

        if (Status != AccountStatus.Active)
            throw new DomainException($"Cannot withdraw from an account with status: {Status}");

        if (Balance < amount)
            throw new DomainException("Insufficient balance for withdrawal");

        Balance -= amount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Block()
    {

        if (Status != AccountStatus.Active)
            throw new DomainException($"Cannot block an account with status: {Status}");

        Status = AccountStatus.Blocked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (Status != AccountStatus.Blocked)
            throw new DomainException($"Cannot activate an account with status: {Status}");

        Status = AccountStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Close()
    {
        if (Status == AccountStatus.Closed)
            throw new DomainException("Account is already closed");

        Status = AccountStatus.Closed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

}