using System.Text.Json;
using FinanceFlow.Application.Interfaces;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace FinanceFlow.Tests.Application.Services;

public class AuditServiceTests
{
    private readonly Mock<IAccountRepository> _mockAccounts;
    private readonly Mock<IAuditLogRepository> _mockAuditLogs;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _mockAccounts = new Mock<IAccountRepository>();
        _mockAuditLogs = new Mock<IAuditLogRepository>();
        _service = new AuditService(_mockAccounts.Object, _mockAuditLogs.Object);
    }

    [Fact]
    public async Task GetAuditLogsByAccountIdAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        var accountId = Guid.NewGuid();

        _mockAccounts
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var act = () => _service.GetAuditLogsByAccountIdAsync(accountId, 1, 20, null, null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAuditLogsByAccountIdAsync_ReturnsMappedResponseWithDeserializedPayload()
    {
        var accountId = Guid.NewGuid();
        var account = Account.Create("Test User", "12345678900");
        const string payload = """
            {
              "EventId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
              "AccountId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              "Type": "Deposit",
              "Amount": 100.50
            }
            """;

        var log = AuditLog.Create(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "finance.transactions.created",
            0,
            1,
            payload);

        _mockAccounts
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _mockAuditLogs
            .Setup(r => r.GetByAccountIdAsync(
                accountId,
                1,
                20,
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<AuditLog>)new[] { log }, 1));

        var (data, totalCount) = await _service.GetAuditLogsByAccountIdAsync(accountId, 1, 20, null, null);

        totalCount.Should().Be(1);
        data.Should().ContainSingle();
        data[0].Payload.ValueKind.Should().Be(JsonValueKind.Object);
        data[0].Payload.GetProperty("Type").GetString().Should().Be("Deposit");
        data[0].Payload.GetProperty("Amount").GetDecimal().Should().Be(100.50m);
    }
}
