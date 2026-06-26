using FinanceFlow.Application.Abstractions;
using FinanceFlow.Application.Interfaces;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceFlow.Tests.Application.Services;

public class TransactionServiceTests
{
    private const string OwnerName = "John Doe";
    private const string DocumentNumber = "12345678900";

    private readonly Mock<IAccountRepository> _accountsMock = new();
    private readonly Mock<ITransactionRepository> _transactionsMock = new();
    private readonly Mock<IEventPublisher> _publisherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDbContextTransaction> _dbTransactionMock;
    private readonly Mock<ILogger<TransactionService>> _loggerMock = new();
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        _unitOfWorkMock = ServiceTestHelper.CreateUnitOfWorkMock(out _dbTransactionMock);

        _sut = new TransactionService(
            _accountsMock.Object,
            _transactionsMock.Object,
            _publisherMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task DepositAsync_WhenAccountExists_UpdatesBalancePersistsTransactionAndCommits()
    {
        var account = Account.Create(OwnerName, DocumentNumber);
        var request = new DepositRequest(100m, "Initial deposit");
        Transaction? capturedTransaction = null;

        _accountsMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionsMock
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((transaction, _) => capturedTransaction = transaction)
            .Returns(Task.CompletedTask);

        var response = await _sut.DepositAsync(account.Id, request, CancellationToken.None);

        account.Balance.Should().Be(100m);
        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.Type.Should().Be(TransactionType.Deposit);
        capturedTransaction.Amount.Should().Be(100m);
        capturedTransaction.BalanceAfter.Should().Be(100m);
        capturedTransaction.Description.Should().Be("Initial deposit");

        response.TransactionId.Should().Be(capturedTransaction.Id);
        response.amount.Should().Be(100m);
        response.NewBalance.Should().Be(100m);

        _accountsMock.Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbTransactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbTransactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DepositAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        var accountId = Guid.NewGuid();
        var request = new DepositRequest(50m, null);

        _accountsMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var act = () => _sut.DepositAsync(accountId, request, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Account {accountId} not found.");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DepositAsync_WhenAmountIsInvalid_RollsBackAndThrowsDomainException()
    {
        var account = Account.Create(OwnerName, DocumentNumber);
        var request = new DepositRequest(0m, null);

        _accountsMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var act = () => _sut.DepositAsync(account.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Deposit amount must be greater than zero");

        _transactionsMock.Verify(
            r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _dbTransactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbTransactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithdrawAsync_WhenAccountHasSufficientBalance_UpdatesBalancePersistsTransactionAndCommits()
    {
        var account = Account.Create(OwnerName, DocumentNumber);
        account.Deposit(200m);
        var request = new WithdrawRequest(75m, "ATM withdrawal");
        Transaction? capturedTransaction = null;

        _accountsMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionsMock
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((transaction, _) => capturedTransaction = transaction)
            .Returns(Task.CompletedTask);

        var response = await _sut.WithdrawAsync(account.Id, request, CancellationToken.None);

        account.Balance.Should().Be(125m);
        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.Type.Should().Be(TransactionType.Withdrawal);
        capturedTransaction.Amount.Should().Be(75m);
        capturedTransaction.BalanceAfter.Should().Be(125m);

        response.TransactionId.Should().Be(capturedTransaction.Id);
        response.amount.Should().Be(75m);
        response.NewBalance.Should().Be(125m);

        _accountsMock.Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _dbTransactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WithdrawAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        var accountId = Guid.NewGuid();
        var request = new WithdrawRequest(10m, null);

        _accountsMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var act = () => _sut.WithdrawAsync(accountId, request, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Account {accountId} not found.");
    }

    [Fact]
    public async Task WithdrawAsync_WhenBalanceIsInsufficient_RollsBackAndThrowsDomainException()
    {
        var account = Account.Create(OwnerName, DocumentNumber);
        account.Deposit(20m);
        var request = new WithdrawRequest(50m, null);

        _accountsMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var act = () => _sut.WithdrawAsync(account.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Insufficient balance for withdrawal");

        _transactionsMock.Verify(
            r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _dbTransactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTransactionsByAccountIdAsync_WhenAccountExists_ReturnsMappedTransactions()
    {
        var account = Account.Create(OwnerName, DocumentNumber);
        var transactions = new List<Transaction>
        {
            Transaction.Create(account.Id, TransactionType.Deposit, 100m, 100m),
            Transaction.Create(account.Id, TransactionType.Withdrawal, 25m, 75m)
        };

        _accountsMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionsMock
            .Setup(r => r.GetByAccountIdAsync(account.Id, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((transactions, transactions.Count));

        var (data, totalCount) = await _sut.GetTransactionsByAccountIdAsync(
            account.Id, 1, 10, CancellationToken.None);

        totalCount.Should().Be(2);
        data.Should().HaveCount(2);
        data[0].amount.Should().Be(100m);
        data[0].NewBalance.Should().Be(100m);
        data[1].amount.Should().Be(25m);
        data[1].NewBalance.Should().Be(75m);
    }

    [Fact]
    public async Task GetTransactionsByAccountIdAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        var accountId = Guid.NewGuid();

        _accountsMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var act = () => _sut.GetTransactionsByAccountIdAsync(accountId, 1, 10, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Account {accountId} not found.");
    }
}
