using FluentAssertions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Tests.Domain.Entities;

public class TransactionTests
{
    private static readonly Guid _accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _relatedAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    #region Transaction.Create Tests

    [Fact]
    public void Create_WithRequiredData_ReturnsTransactionWithExpectedProperties()
    {
        // Arrange
        const decimal amount = 150.50m;
        const decimal balanceAfter = 650.50m;

        // Act
        var transaction = Transaction.Create(
            accountId: _accountId,
            type: TransactionType.Deposit,
            amount: amount,
            balanceAfter: balanceAfter);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Id.Should().NotBeEmpty();
        transaction.AccountId.Should().Be(_accountId);
        transaction.Type.Should().Be(TransactionType.Deposit);
        transaction.Amount.Should().Be(amount);
        transaction.BalanceAfter.Should().Be(balanceAfter);
        transaction.RelatedAccountId.Should().BeNull();
        transaction.Description.Should().BeNull();
        transaction.CreatedAt.Should().BeBefore(DateTimeOffset.UtcNow.AddSeconds(1));
        transaction.CreatedAt.Should().BeAfter(DateTimeOffset.UtcNow.AddSeconds(-5));
    }

    [Theory]
    [InlineData(TransactionType.Deposit)]
    [InlineData(TransactionType.Withdrawal)]
    [InlineData(TransactionType.TransferIn)]
    [InlineData(TransactionType.TransferOut)]
    public void Create_WithEachTransactionType_SetsTypeCorrectly(TransactionType type)
    {
        // Act
        var transaction = Transaction.Create(
            accountId: _accountId,
            type: type,
            amount: 100m,
            balanceAfter: 100m);

        // Assert
        transaction.Type.Should().Be(type);
    }

    [Fact]
    public void Create_WithRelatedAccountId_SetsRelatedAccountId()
    {
        // Act
        var transaction = Transaction.Create(
            accountId: _accountId,
            type: TransactionType.TransferOut,
            amount: 75m,
            balanceAfter: 425m,
            relatedAccountId: _relatedAccountId);

        // Assert
        transaction.RelatedAccountId.Should().Be(_relatedAccountId);
    }

    [Fact]
    public void Create_WithDescription_SetsDescription()
    {
        // Arrange
        const string description = "Monthly savings deposit";

        // Act
        var transaction = Transaction.Create(
            accountId: _accountId,
            type: TransactionType.Deposit,
            amount: 200m,
            balanceAfter: 200m,
            description: description);

        // Assert
        transaction.Description.Should().Be(description);
    }

    [Fact]
    public void Create_WithAllOptionalFields_SetsAllProperties()
    {
        // Arrange
        const string description = "Transfer between accounts";

        // Act
        var transaction = Transaction.Create(
            accountId: _accountId,
            type: TransactionType.TransferIn,
            amount: 300m,
            balanceAfter: 1300m,
            relatedAccountId: _relatedAccountId,
            description: description);

        // Assert
        transaction.AccountId.Should().Be(_accountId);
        transaction.Type.Should().Be(TransactionType.TransferIn);
        transaction.Amount.Should().Be(300m);
        transaction.BalanceAfter.Should().Be(1300m);
        transaction.RelatedAccountId.Should().Be(_relatedAccountId);
        transaction.Description.Should().Be(description);
    }

    [Fact]
    public void Create_CalledTwice_GeneratesUniqueIds()
    {
        // Act
        var first = Transaction.Create(_accountId, TransactionType.Deposit, 10m, 10m);
        var second = Transaction.Create(_accountId, TransactionType.Deposit, 10m, 20m);

        // Assert
        first.Id.Should().NotBe(second.Id);
    }

    #endregion
}
