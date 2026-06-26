using FluentAssertions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Infrastructure.Data;
using FinanceFlow.Infrastructure.Data.Repositories;
using FinanceFlow.Tests.Infrastructure.Integration;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Tests.Infrastructure.Data.Repositories;

[Collection(nameof(PostgreSqlCollection))]
public sealed class AccountRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private FinanceFlowDbContext _dbContext = null!;
    private AccountRepository _sut = null!;

    public AccountRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _sut = new AccountRepository(_dbContext);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE accounts CASCADE");
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_PersistsAccount()
    {
        var account = Account.Create("John Doe", "12345678900");

        await _sut.AddAsync(account, CancellationToken.None);

        var persisted = await _dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(a => a.Id == account.Id);

        persisted.OwnerName.Should().Be("John Doe");
        persisted.DocumentNumber.Should().Be("12345678900");
        persisted.Balance.Should().Be(0m);
        persisted.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAccountExists_ReturnsAccount()
    {
        var account = Account.Create("Jane Doe", "98765432100");
        await _sut.AddAsync(account, CancellationToken.None);

        var result = await _sut.GetByIdAsync(account.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
        result.OwnerName.Should().Be("Jane Doe");
        result.DocumentNumber.Should().Be("98765432100");
    }

    [Fact]
    public async Task GetByIdAsync_WhenAccountDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByDocumentAsync_WhenAccountExists_ReturnsAccount()
    {
        var account = Account.Create("Alice Smith", "11122233344");
        await _sut.AddAsync(account, CancellationToken.None);

        var result = await _sut.GetByDocumentAsync("11122233344", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
        result.DocumentNumber.Should().Be("11122233344");
    }

    [Fact]
    public async Task GetByDocumentAsync_WhenAccountDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByDocumentAsync("00000000000", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithoutStatusFilter_ReturnsAllAccounts()
    {
        var active = Account.Create("Active User", "10000000001");
        var blocked = Account.Create("Blocked User", "10000000002");
        blocked.Block();

        await _sut.AddAsync(active, CancellationToken.None);
        await _sut.AddAsync(blocked, CancellationToken.None);

        var result = await _sut.GetAllAsync(ct: CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(a => a.Id).Should().BeEquivalentTo([active.Id, blocked.Id]);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsOnlyMatchingAccounts()
    {
        var active = Account.Create("Active User", "20000000001");
        var blocked = Account.Create("Blocked User", "20000000002");
        blocked.Block();

        await _sut.AddAsync(active, CancellationToken.None);
        await _sut.AddAsync(blocked, CancellationToken.None);

        var result = await _sut.GetAllAsync(AccountStatus.Blocked, CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().Id.Should().Be(blocked.Id);
        result.Single().Status.Should().Be(AccountStatus.Blocked);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var account = Account.Create("Update User", "30000000001");
        await _sut.AddAsync(account, CancellationToken.None);

        account.Deposit(150m);
        await _sut.UpdateAsync(account, CancellationToken.None);

        var persisted = await _dbContext.Accounts
            .AsNoTracking()
            .SingleAsync(a => a.Id == account.Id);

        persisted.Balance.Should().Be(150m);
        persisted.UpdatedAt.Should().BeAfter(persisted.CreatedAt);
    }

    [Fact]
    public async Task SelectForUpdate_WithinTransaction_ReturnsBothAccounts()
    {
        var first = Account.Create("Source User", "40000000001");
        var second = Account.Create("Destination User", "40000000002");

        await _sut.AddAsync(first, CancellationToken.None);
        await _sut.AddAsync(second, CancellationToken.None);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var result = await _sut.SelectForUpdate(first.Id, second.Id, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainKeys(first.Id, second.Id);
        result[first.Id].OwnerName.Should().Be("Source User");
        result[second.Id].OwnerName.Should().Be("Destination User");
    }

    [Fact]
    public async Task SelectForUpdate_WhenOneAccountMissing_ReturnsOnlyExistingAccount()
    {
        var existing = Account.Create("Existing User", "50000000001");
        await _sut.AddAsync(existing, CancellationToken.None);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var result = await _sut.SelectForUpdate(existing.Id, Guid.NewGuid(), CancellationToken.None);

        result.Should().ContainSingle();
        result.Should().ContainKey(existing.Id);
    }
}
