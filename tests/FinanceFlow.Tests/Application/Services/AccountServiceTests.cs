using FinanceFlow.Application.Interfaces;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace FinanceFlow.Tests.Application.Services;

public class AccountServiceTests
{
    private const string OwnerName = "John Doe";
    private const string DocumentNumber = "12345678900";

    private readonly Mock<IAccountRepository> _repositoryMock = new();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _sut = new AccountService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsAccountAndReturnsResponse()
    {
        var request = new CreateAccountRequest(OwnerName, DocumentNumber);
        Account? capturedAccount = null;

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((account, _) => capturedAccount = account)
            .Returns(Task.CompletedTask);

        var response = await _sut.CreateAsync(request, CancellationToken.None);

        capturedAccount.Should().NotBeNull();
        capturedAccount!.OwnerName.Should().Be(OwnerName);
        capturedAccount.DocumentNumber.Should().Be(DocumentNumber);
        capturedAccount.Balance.Should().Be(0m);
        capturedAccount.Status.Should().Be(AccountStatus.Active);

        response.Id.Should().Be(capturedAccount.Id);
        response.OwnerName.Should().Be(OwnerName);
        response.DocumentNumber.Should().Be(DocumentNumber);
        response.Balance.Should().Be(0m);
        response.Status.Should().Be(AccountStatus.Active.ToString());

        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAccountExists_ReturnsMappedResponse()
    {
        var account = Account.Create(OwnerName, DocumentNumber);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var response = await _sut.GetByIdAsync(account.Id, CancellationToken.None);

        response.Should().NotBeNull();
        response!.Id.Should().Be(account.Id);
        response.OwnerName.Should().Be(OwnerName);
        response.DocumentNumber.Should().Be(DocumentNumber);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAccountDoesNotExist_ReturnsNull()
    {
        var accountId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var response = await _sut.GetByIdAsync(accountId, CancellationToken.None);

        response.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithoutStatusFilter_ReturnsAllAccounts()
    {
        var active = Account.Create("Alice", "11111111111");
        var blocked = Account.Create("Bob", "22222222222");
        blocked.Block();

        _repositoryMock
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { active, blocked });

        var responses = (await _sut.GetAllAsync(null, CancellationToken.None)).ToList();

        responses.Should().HaveCount(2);
        responses.Should().Contain(r => r.Id == active.Id);
        responses.Should().Contain(r => r.Id == blocked.Id);
    }

    [Fact]
    public async Task GetAllAsync_WithValidStatusFilter_ReturnsMatchingAccounts()
    {
        var active = Account.Create("Alice", "11111111111");
        var blocked = Account.Create("Bob", "22222222222");
        blocked.Block();

        _repositoryMock
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { active, blocked });

        var responses = (await _sut.GetAllAsync("Blocked", CancellationToken.None)).ToList();

        responses.Should().ContainSingle();
        responses[0].Id.Should().Be(blocked.Id);
        responses[0].Status.Should().Be(AccountStatus.Blocked.ToString());
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidStatusFilter_ReturnsAllAccounts()
    {
        var active = Account.Create("Alice", "11111111111");
        var blocked = Account.Create("Bob", "22222222222");
        blocked.Block();

        _repositoryMock
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { active, blocked });

        var responses = (await _sut.GetAllAsync("InvalidStatus", CancellationToken.None)).ToList();

        responses.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        var accountId = Guid.NewGuid();
        var request = new UpdateAccountStatusRequest("Blocked");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var act = () => _sut.UpdateStatusAsync(accountId, request, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Account {accountId} not found");
    }

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidStatus_ThrowsDomainException()
    {
        var account = Account.Create(OwnerName, DocumentNumber);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var act = () => _sut.UpdateStatusAsync(
            account.Id,
            new UpdateAccountStatusRequest("InvalidStatus"),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Status 'InvalidStatus' is not a valid status.");
    }

    [Fact]
    public async Task UpdateStatusAsync_BlockActiveAccount_UpdatesRepositoryAndReturnsBlockedStatus()
    {
        var account = Account.Create(OwnerName, DocumentNumber);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var response = await _sut.UpdateStatusAsync(
            account.Id,
            new UpdateAccountStatusRequest("Blocked"),
            CancellationToken.None);

        response.Status.Should().Be(AccountStatus.Blocked.ToString());
        account.Status.Should().Be(AccountStatus.Blocked);

        _repositoryMock.Verify(
            r => r.UpdateAsync(account, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ActivateBlockedAccount_UpdatesRepositoryAndReturnsActiveStatus()
    {
        var account = Account.Create(OwnerName, DocumentNumber);
        account.Block();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var response = await _sut.UpdateStatusAsync(
            account.Id,
            new UpdateAccountStatusRequest("Active"),
            CancellationToken.None);

        response.Status.Should().Be(AccountStatus.Active.ToString());
        account.Status.Should().Be(AccountStatus.Active);

        _repositoryMock.Verify(
            r => r.UpdateAsync(account, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_CloseAccount_UpdatesRepositoryAndReturnsClosedStatus()
    {
        var account = Account.Create(OwnerName, DocumentNumber);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var response = await _sut.UpdateStatusAsync(
            account.Id,
            new UpdateAccountStatusRequest("Closed"),
            CancellationToken.None);

        response.Status.Should().Be(AccountStatus.Closed.ToString());
        account.Status.Should().Be(AccountStatus.Closed);

        _repositoryMock.Verify(
            r => r.UpdateAsync(account, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
