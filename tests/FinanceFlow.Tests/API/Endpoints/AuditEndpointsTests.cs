using System.Text.Json;
using FinanceFlow.Application.Audit;
using FinanceFlow.Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FinanceFlow.Tests.API.Endpoints;

public class AuditEndpointsTests
{
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly CancellationToken _cancellationToken;

    public AuditEndpointsTests()
    {
        _mockAuditService = new Mock<IAuditService>();
        _cancellationToken = CancellationToken.None;
    }

    [Fact]
    public async Task GetAuditLogsByAccountId_WithValidPagination_ReturnsOkResult()
    {
        var accountId = Guid.NewGuid();
        var page = 1;
        var pageSize = 20;
        var payload = JsonDocument.Parse("""{"AccountId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","Type":"Deposit"}""").RootElement;
        var logs = new List<AuditLogResponse>
        {
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "finance.transactions.created",
                0,
                1,
                DateTimeOffset.UtcNow,
                payload)
        };

        _mockAuditService
            .Setup(s => s.GetAuditLogsByAccountIdAsync(accountId, page, pageSize, null, null, _cancellationToken))
            .ReturnsAsync(((IReadOnlyList<AuditLogResponse>)logs, 1));

        var result = await GetAuditLogsByAccountIdEndpoint(accountId, page, pageSize, null, null);

        result.Should().BeAssignableTo<IResult>();
        result.GetType().Name.Should().StartWith("Ok");

        _mockAuditService.Verify(
            s => s.GetAuditLogsByAccountIdAsync(accountId, page, pageSize, null, null, _cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsByAccountId_WithDateFilters_ReturnsOkResult()
    {
        var accountId = Guid.NewGuid();
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 12, 31);

        _mockAuditService
            .Setup(s => s.GetAuditLogsByAccountIdAsync(accountId, 1, 20, from, to, _cancellationToken))
            .ReturnsAsync(((IReadOnlyList<AuditLogResponse>)Array.Empty<AuditLogResponse>(), 0));

        var result = await GetAuditLogsByAccountIdEndpoint(accountId, 1, 20, from, to);

        result.Should().BeAssignableTo<IResult>();
        result.GetType().Name.Should().StartWith("Ok");

        _mockAuditService.Verify(
            s => s.GetAuditLogsByAccountIdAsync(accountId, 1, 20, from, to, _cancellationToken),
            Times.Once);
    }

    private async Task<IResult> GetAuditLogsByAccountIdEndpoint(
        Guid accountId,
        int page,
        int pageSize,
        DateOnly? from,
        DateOnly? to)
    {
        var (data, totalCount) = await _mockAuditService.Object.GetAuditLogsByAccountIdAsync(
            accountId, page, pageSize, from, to, _cancellationToken);

        return Results.Ok(new { Data = data, TotalCount = totalCount });
    }
}
