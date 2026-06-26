using FluentAssertions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Infrastructure.Data;
using FinanceFlow.Tests.Infrastructure.Integration;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Tests.Infrastructure.Data;

[Collection(nameof(PostgreSqlCollection))]
public sealed class FinanceFlowDbContextTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private FinanceFlowDbContext _dbContext = null!;

    public FinanceFlowDbContextTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE accounts CASCADE");
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task Database_WhenMigrated_ExposesAccountsAndTransactionsDbSets()
    {
        (await _dbContext.Database.CanConnectAsync()).Should().BeTrue();

        var account = Account.Create("DbSet User", "90000000001");
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        var transaction = Transaction.Create(
            account.Id, TransactionType.Deposit, 100m, 100m);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        (await _dbContext.Accounts.CountAsync()).Should().Be(1);
        (await _dbContext.Transactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsAccountThroughDbSet()
    {
        var account = Account.Create("John Doe", "12345678900");

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        var persisted = await _dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(a => a.Id == account.Id);

        persisted.OwnerName.Should().Be("John Doe");
        persisted.DocumentNumber.Should().Be("12345678900");
        persisted.Balance.Should().Be(0m);
        persisted.Status.Should().Be(AccountStatus.Active);
        persisted.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        persisted.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsTransactionLinkedToAccount()
    {
        var account = await PersistAccountAsync("Linked User", "90000000002");
        var transaction = Transaction.Create(
            account.Id,
            TransactionType.Withdrawal,
            50m,
            50m,
            description: "ATM withdrawal");

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        var persisted = await _dbContext.Transactions
            .AsNoTracking()
            .SingleAsync(t => t.Id == transaction.Id);

        persisted.AccountId.Should().Be(account.Id);
        persisted.Type.Should().Be(TransactionType.Withdrawal);
        persisted.Amount.Should().Be(50m);
        persisted.BalanceAfter.Should().Be(50m);
        persisted.Description.Should().Be("ATM withdrawal");
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDocumentNumberIsDuplicate_ThrowsDbUpdateException()
    {
        var first = Account.Create("First User", "11111111111");
        var duplicate = Account.Create("Second User", "11111111111");

        _dbContext.Accounts.Add(first);
        await _dbContext.SaveChangesAsync();

        _dbContext.Accounts.Add(duplicate);

        var act = () => _dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenTransactionReferencesNonExistentAccount_ThrowsDbUpdateException()
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(), TransactionType.Deposit, 100m, 100m);

        _dbContext.Transactions.Add(transaction);

        var act = () => _dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDeletingAccountWithTransactions_ThrowsDbUpdateException()
    {
        var account = await PersistAccountAsync("Protected User", "90000000003");
        var transaction = Transaction.Create(
            account.Id, TransactionType.Deposit, 100m, 100m);

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        await using var deleteContext = _fixture.CreateDbContext();
        var accountToDelete = await deleteContext.Accounts.SingleAsync(a => a.Id == account.Id);
        deleteContext.Accounts.Remove(accountToDelete);

        var act = () => deleteContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDeletingAccountWithoutTransactions_RemovesAccount()
    {
        var account = await PersistAccountAsync("Deletable User", "90000000004");

        _dbContext.Accounts.Remove(account);
        await _dbContext.SaveChangesAsync();

        (await _dbContext.Accounts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsAccountStatusAsString()
    {
        var account = Account.Create("Status User", "90000000005");
        account.Block();

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        var persistedStatus = await _dbContext.Database
            .SqlQueryRaw<string>("SELECT status AS \"Value\" FROM accounts WHERE id = {0}", account.Id)
            .SingleAsync();

        persistedStatus.Should().Be(nameof(AccountStatus.Blocked));
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsTransactionTypeAsString()
    {
        var account = await PersistAccountAsync("Type User", "90000000006");
        var transaction = Transaction.Create(
            account.Id, TransactionType.TransferIn, 200m, 200m);

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        var persistedType = await _dbContext.Database
            .SqlQueryRaw<string>("SELECT type AS \"Value\" FROM transactions WHERE id = {0}", transaction.Id)
            .SingleAsync();

        persistedType.Should().Be(nameof(TransactionType.TransferIn));
    }

    [Fact]
    public async Task SaveChangesAsync_WhenOwnerNameExceedsMaxLength_ThrowsDbUpdateException()
    {
        var account = Account.Create(new string('A', 101), "90000000007");

        _dbContext.Accounts.Add(account);

        var act = () => _dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    private async Task<Account> PersistAccountAsync(string ownerName, string document)
    {
        var account = Account.Create(ownerName, document);
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();
        return account;
    }
}
