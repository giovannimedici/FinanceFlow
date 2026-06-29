using FinanceFlow.Application.Abstractions;
using FinanceFlow.Application.Events;
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

public class TransferServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionsMock = new();
    private readonly Mock<IEventPublisher> _publisherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDbContextTransaction> _dbTransactionMock;
    private readonly Mock<ILogger<TransferService>> _loggerMock = new();
    private readonly Mock<IAccountRepository> _accountsMock = new();
    private readonly TransferService _sut;

    public TransferServiceTests()
    {
        _unitOfWorkMock = ServiceTestHelper.CreateUnitOfWorkMock(out _dbTransactionMock);

        _publisherMock
            .Setup(p => p.PublishAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TransactionCreatedEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new TransferService(
            _transactionsMock.Object,
            _publisherMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _accountsMock.Object);
    }

    [Fact]
    public async Task TransferAsync_WithValidRequest_TransfersBalancePersistsTransactionsAndCommits()
    {
        var source = CreateAccountWithBalance("Alice", "11111111111", 500m);
        var destination = CreateAccountWithBalance("Bob", "22222222222", 100m);
        var request = new TransferRequest(source.Id, destination.Id, 150m, "Rent payment");

        SetupSelectForUpdate(source, destination);

        var capturedTransactions = new List<Transaction>();
        _transactionsMock
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((transaction, _) => capturedTransactions.Add(transaction))
            .Returns(Task.CompletedTask);

        var response = await _sut.TransferAsync(request, CancellationToken.None);

        source.Balance.Should().Be(350m);
        destination.Balance.Should().Be(250m);
        response.SourceBalance.Should().Be(350m);
        response.DestinationBalance.Should().Be(250m);
        response.TransferId.Should().NotBeEmpty();

        capturedTransactions.Should().HaveCount(2);
        capturedTransactions.Should().Contain(t =>
            t.AccountId == source.Id &&
            t.Type == TransactionType.TransferOut &&
            t.Amount == 150m &&
            t.RelatedAccountId == destination.Id);
        capturedTransactions.Should().Contain(t =>
            t.AccountId == destination.Id &&
            t.Type == TransactionType.TransferIn &&
            t.Amount == 150m &&
            t.RelatedAccountId == source.Id);

        _accountsMock.Verify(r => r.UpdateAsync(source, It.IsAny<CancellationToken>()), Times.Once);
        _accountsMock.Verify(r => r.UpdateAsync(destination, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbTransactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(
            p => p.PublishAsync(
                It.IsAny<Guid>(),
                KafkaTopics.TransactionsCreated,
                It.IsAny<string>(),
                It.IsAny<TransactionCreatedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task TransferAsync_UsesOrderedAccountIdsWhenSelectingForUpdate()
    {
        var source = CreateAccountWithBalance("Alice", "11111111111", 300m);
        var destination = CreateAccountWithBalance("Bob", "22222222222", 50m);
        var request = new TransferRequest(source.Id, destination.Id, 100m, null);

        var orderedIds = new[] { source.Id, destination.Id }.OrderBy(id => id).ToArray();
        SetupSelectForUpdate(source, destination);

        await _sut.TransferAsync(request, CancellationToken.None);

        _accountsMock.Verify(
            r => r.SelectForUpdate(orderedIds[0], orderedIds[1], It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TransferAsync_WhenSourceAccountNotFound_RollsBackAndThrowsDomainException()
    {
        var sourceId = Guid.NewGuid();
        var destination = CreateAccountWithBalance("Bob", "22222222222", 100m);
        var request = new TransferRequest(sourceId, destination.Id, 50m, null);

        var orderedIds = new[] { sourceId, destination.Id }.OrderBy(id => id).ToArray();
        var accounts = new Dictionary<Guid, Account> { [destination.Id] = destination };

        _accountsMock
            .Setup(r => r.SelectForUpdate(orderedIds[0], orderedIds[1], It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var act = () => _sut.TransferAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Source account not found.");

        _dbTransactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbTransactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransferAsync_WhenDestinationAccountNotFound_RollsBackAndThrowsDomainException()
    {
        var source = CreateAccountWithBalance("Alice", "11111111111", 300m);
        var destinationId = Guid.NewGuid();
        var request = new TransferRequest(source.Id, destinationId, 50m, null);

        var orderedIds = new[] { source.Id, destinationId }.OrderBy(id => id).ToArray();
        var accounts = new Dictionary<Guid, Account> { [source.Id] = source };

        _accountsMock
            .Setup(r => r.SelectForUpdate(orderedIds[0], orderedIds[1], It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var act = () => _sut.TransferAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Destination account not found.");

        _dbTransactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferAsync_WhenSourceAccountIsNotActive_RollsBackAndThrowsDomainException()
    {
        var source = CreateAccountWithBalance("Alice", "11111111111", 300m);
        source.Block();
        var destination = CreateAccountWithBalance("Bob", "22222222222", 100m);
        var request = new TransferRequest(source.Id, destination.Id, 50m, null);

        SetupSelectForUpdate(source, destination);

        var act = () => _sut.TransferAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Source account is not active.");

        _transactionsMock.Verify(
            r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _dbTransactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferAsync_WhenDestinationAccountIsNotActive_RollsBackAndThrowsDomainException()
    {
        var source = CreateAccountWithBalance("Alice", "11111111111", 300m);
        var destination = CreateAccountWithBalance("Bob", "22222222222", 100m);
        destination.Block();
        var request = new TransferRequest(source.Id, destination.Id, 50m, null);

        SetupSelectForUpdate(source, destination);

        var act = () => _sut.TransferAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Destination account is not active.");

        _transactionsMock.Verify(
            r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _dbTransactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferAsync_WhenSourceHasInsufficientBalance_RollsBackAndThrowsDomainException()
    {
        var source = CreateAccountWithBalance("Alice", "11111111111", 40m);
        var destination = CreateAccountWithBalance("Bob", "22222222222", 100m);
        var request = new TransferRequest(source.Id, destination.Id, 50m, null);

        SetupSelectForUpdate(source, destination);

        var act = () => _sut.TransferAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Insufficient balance for withdrawal");

        source.Balance.Should().Be(40m);
        destination.Balance.Should().Be(100m);
        _dbTransactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupSelectForUpdate(Account source, Account destination)
    {
        var orderedIds = new[] { source.Id, destination.Id }.OrderBy(id => id).ToArray();
        var accounts = new Dictionary<Guid, Account>
        {
            [source.Id] = source,
            [destination.Id] = destination
        };

        _accountsMock
            .Setup(r => r.SelectForUpdate(orderedIds[0], orderedIds[1], It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);
    }

    private static Account CreateAccountWithBalance(string ownerName, string documentNumber, decimal balance)
    {
        var account = Account.Create(ownerName, documentNumber);
        if (balance > 0)
            account.Deposit(balance);

        return account;
    }
}
