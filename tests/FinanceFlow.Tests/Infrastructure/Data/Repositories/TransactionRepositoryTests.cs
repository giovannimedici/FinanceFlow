using FluentAssertions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Infrastructure.Data;
using FinanceFlow.Infrastructure.Data.Repositories;
using FinanceFlow.Tests.Infrastructure.Integration;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Tests.Infrastructure.Data.Repositories;

[Collection(nameof(PostgreSqlCollection))]
public sealed class TransactionRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private FinanceFlowDbContext _dbContext = null!;
    private TransactionRepository _sut = null!;

    public TransactionRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _sut = new TransactionRepository(_dbContext);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE accounts CASCADE");
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_WhenSaved_PersistsTransaction()
    {
        var account = await CreateAccountAsync();
        var transaction = Transaction.Create(
            accountId: account.Id,
            type: TransactionType.Deposit,
            amount: 250m,
            balanceAfter: 250m);

        await PersistTransactionAsync(transaction);

        var persisted = await _dbContext.Transactions
            .AsNoTracking()
            .SingleAsync(t => t.Id == transaction.Id);

        persisted.AccountId.Should().Be(account.Id);
        persisted.Type.Should().Be(TransactionType.Deposit);
        persisted.Amount.Should().Be(250m);
        persisted.BalanceAfter.Should().Be(250m);
        persisted.RelatedAccountId.Should().BeNull();
        persisted.Description.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithOptionalFields_PersistsRelatedAccountAndDescription()
    {
        var account = await CreateAccountAsync("Source User", "11111111111");
        var relatedAccount = await CreateAccountAsync("Destination User", "22222222222");
        const string description = "Transfer between accounts";

        var transaction = Transaction.Create(
            accountId: account.Id,
            type: TransactionType.TransferOut,
            amount: 75m,
            balanceAfter: 425m,
            relatedAccountId: relatedAccount.Id,
            description: description);

        await PersistTransactionAsync(transaction);

        var persisted = await _dbContext.Transactions
            .AsNoTracking()
            .SingleAsync(t => t.Id == transaction.Id);

        persisted.RelatedAccountId.Should().Be(relatedAccount.Id);
        persisted.Description.Should().Be(description);
    }

    [Fact]
    public async Task GetByAccountIdAsync_WhenAccountHasTransactions_ReturnsOnlyMatchingTransactions()
    {
        var targetAccount = await CreateAccountAsync("Target User", "33333333333");
        var otherAccount = await CreateAccountAsync("Other User", "44444444444");

        var targetTransaction = Transaction.Create(
            targetAccount.Id, TransactionType.Deposit, 100m, 100m);
        var otherTransaction = Transaction.Create(
            otherAccount.Id, TransactionType.Deposit, 50m, 50m);

        await PersistTransactionAsync(targetTransaction);
        await PersistTransactionAsync(otherTransaction);

        var (data, totalCount) = await _sut.GetByAccountIdAsync(
            targetAccount.Id, page: 1, pageSize: 10, CancellationToken.None);

        totalCount.Should().Be(1);
        data.Should().ContainSingle();
        data.Single().Id.Should().Be(targetTransaction.Id);
        data.Single().AccountId.Should().Be(targetAccount.Id);
    }

    [Fact]
    public async Task GetByAccountIdAsync_WhenAccountHasNoTransactions_ReturnsEmptyListAndZeroTotal()
    {
        var account = await CreateAccountAsync("Empty User", "55555555555");

        var (data, totalCount) = await _sut.GetByAccountIdAsync(
            account.Id, page: 1, pageSize: 10, CancellationToken.None);

        data.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByAccountIdAsync_OrdersByCreatedAtDescending()
    {
        var account = await CreateAccountAsync("Ordered User", "66666666666");
        var oldest = Transaction.Create(account.Id, TransactionType.Deposit, 10m, 10m);
        var middle = Transaction.Create(account.Id, TransactionType.Deposit, 20m, 30m);
        var newest = Transaction.Create(account.Id, TransactionType.Deposit, 30m, 60m);

        await PersistTransactionAsync(oldest, new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));
        await PersistTransactionAsync(middle, new DateTimeOffset(2024, 1, 2, 10, 0, 0, TimeSpan.Zero));
        await PersistTransactionAsync(newest, new DateTimeOffset(2024, 1, 3, 10, 0, 0, TimeSpan.Zero));

        var (data, _) = await _sut.GetByAccountIdAsync(
            account.Id, page: 1, pageSize: 10, CancellationToken.None);

        data.Select(t => t.Id).Should().Equal(newest.Id, middle.Id, oldest.Id);
    }

    [Fact]
    public async Task GetByAccountIdAsync_WithPagination_ReturnsCorrectPageAndTotalCount()
    {
        var account = await CreateAccountAsync("Paged User", "77777777777");

        for (var i = 1; i <= 5; i++)
        {
            var transaction = Transaction.Create(account.Id, TransactionType.Deposit, i * 10m, i * 10m);
            await PersistTransactionAsync(
                transaction,
                new DateTimeOffset(2024, 1, i, 10, 0, 0, TimeSpan.Zero));
        }

        var (firstPage, totalCount) = await _sut.GetByAccountIdAsync(
            account.Id, page: 1, pageSize: 2, CancellationToken.None);

        totalCount.Should().Be(5);
        firstPage.Should().HaveCount(2);

        var (secondPage, secondTotalCount) = await _sut.GetByAccountIdAsync(
            account.Id, page: 2, pageSize: 2, CancellationToken.None);

        secondTotalCount.Should().Be(5);
        secondPage.Should().HaveCount(2);
        firstPage.Select(t => t.Id).Should().NotIntersectWith(secondPage.Select(t => t.Id));
    }

    [Fact]
    public async Task GetByAccountIdAsync_WhenPageExceedsResults_ReturnsEmptyDataWithCorrectTotalCount()
    {
        var account = await CreateAccountAsync("Last Page User", "88888888888");
        var transaction = Transaction.Create(account.Id, TransactionType.Deposit, 100m, 100m);
        await PersistTransactionAsync(transaction);

        var (data, totalCount) = await _sut.GetByAccountIdAsync(
            account.Id, page: 2, pageSize: 10, CancellationToken.None);

        data.Should().BeEmpty();
        totalCount.Should().Be(1);
    }

    private async Task<Account> CreateAccountAsync(string ownerName = "Test User", string document = "12345678900")
    {
        var account = Account.Create(ownerName, document);
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();
        return account;
    }

    private async Task PersistTransactionAsync(Transaction transaction, DateTimeOffset? createdAt = null)
    {
        await _sut.AddAsync(transaction, CancellationToken.None);

        if (createdAt.HasValue)
        {
            _dbContext.Entry(transaction).Property("CreatedAt").CurrentValue = createdAt.Value;
        }

        await _dbContext.SaveChangesAsync();
    }
}
