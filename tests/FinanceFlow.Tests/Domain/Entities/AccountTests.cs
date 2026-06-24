using Xunit;
using FluentAssertions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Exceptions;
using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Tests.Domain.Entities;

public class AccountTests
{
    private const string _validOwnerName = "John Doe";
    private const string _validDocumentNumber = "12345678900";

    #region Account.Create Tests

    [Fact]
    public void Create_WithValidData_ReturnsAccountWithZeroBalance()
    {
        // Act
        var account = Account.Create(_validOwnerName, _validDocumentNumber);

        // Assert
        account.Should().NotBeNull();
        account.Id.Should().NotBeEmpty();
        account.OwnerName.Should().Be(_validOwnerName);
        account.DocumentNumber.Should().Be(_validDocumentNumber);
        account.Balance.Should().Be(0m);
        account.Status.Should().Be(AccountStatus.Active);
        account.CreatedAt.Should().BeBefore(DateTimeOffset.UtcNow.AddSeconds(1));
        account.UpdatedAt.Should().Be(account.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyOwnerName_ThrowsDomainException(string in_validOwnerName)
    {
        // Act
        Action act = () => Account.Create(in_validOwnerName, _validDocumentNumber);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Owner name is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyDocumentNumber_ThrowsDomainException(string in_validDocumentNumber)
    {
        // Act
        Action act = () => Account.Create(_validOwnerName, in_validDocumentNumber);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Document number is required");
    }

    #endregion

    #region Deposit Tests

    [Fact]
    public void Deposit_PositiveAmount_UpdatesBalance()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);
        var depositAmount = 150.50m;

        // Act
        account.Deposit(depositAmount);

        // Assert
        account.Balance.Should().Be(depositAmount);
    }

    [Fact]
    public void Deposit_ZeroAmount_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);

        // Act
        Action act = () => account.Deposit(0m);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Deposit amount must be greater than zero");
    }

    [Fact]
    public void Deposit_NegativeAmount_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);

        // Act
        Action act = () => account.Deposit(-50m);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Deposit amount must be greater than zero");
    }

    [Fact]
    public void Deposit_BlockedAccount_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);
        account.Block();

        // Act
        Action act = () => account.Deposit(100m);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage($"Cannot deposit into an account with status: {AccountStatus.Blocked}");
    }

    #endregion

    #region Withdraw Tests

    [Fact]
    public void Withdraw_SufficientBalance_DeductsAmount()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);
        account.Deposit(500m);
        var withdrawalAmount = 200m;

        // Act
        account.Withdraw(withdrawalAmount);

        // Assert
        account.Balance.Should().Be(300m);
    }

    [Fact]
    public void Withdraw_InsufficientBalance_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);
        account.Deposit(100m);

        // Act
        Action act = () => account.Withdraw(150m);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Insufficient balance for withdrawal");
    }

    [Fact]
    public void Withdraw_ClosedAccount_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);
        account.Close();

        // Act
        Action act = () => account.Withdraw(50m);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage($"Cannot withdraw from an account with status: {AccountStatus.Closed}");
    }

    #endregion

    #region Status Transitions Tests

    [Fact]
    public void Block_ActiveAccount_SetsStatusToBlocked()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);

        // Act
        account.Block();

        // Assert
        account.Status.Should().Be(AccountStatus.Blocked);
    }

    [Fact]
    public void Activate_BlockedAccount_SetsStatusToActive()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);
        account.Block();

        // Act
        account.Activate();

        // Assert
        account.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public void Close_ClosedAccount_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);
        account.Close();

        // Act
        Action act = () => account.Close();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Account is already closed");
    }

    [Fact]
    public void Close_ActiveAccount_SetsStatusToClosed()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);

        // Act
        account.Close();

        // Assert
        account.Status.Should().Be(AccountStatus.Closed);
    }

    [Fact]
    public void Activate_ClosedAccount_ThrowsDomainException()
    {
        // Arrange
        var account = Account.Create(_validOwnerName, _validDocumentNumber);
        account.Close();

        // Act
        Action act = () => account.Activate();

        // Assert
        act.Should().Throw<DomainException>();
    }
    #endregion
}