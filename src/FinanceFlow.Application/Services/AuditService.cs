using System.Text.Json;
using FinanceFlow.Application.Audit;
using FinanceFlow.Application.Interfaces;
using FinanceFlow.Application.Services.Interfaces;
using FinanceFlow.Domain.Exceptions;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Services;

public sealed class AuditService : IAuditService
{
    private readonly IAccountRepository _accounts;
    private readonly IAuditLogRepository _auditLogs;

    public AuditService(IAccountRepository accounts, IAuditLogRepository auditLogs)
    {
        _accounts = accounts;
        _auditLogs = auditLogs;
    }

    public async Task<(IReadOnlyList<AuditLogResponse> Data, int TotalCount)> GetAuditLogsByAccountIdAsync(
        Guid accountId,
        int page,
        int pageSize,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct = default)
    {
        _ = await _accounts.GetByIdAsync(accountId, ct)
            ?? throw new NotFoundException($"Account {accountId} not found.");

        DateTimeOffset? fromDt = from.HasValue
            ? new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), TimeSpan.Zero)
            : null;

        DateTimeOffset? toExclusive = to.HasValue
            ? new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), TimeSpan.Zero)
            : null;

        var (data, totalCount) = await _auditLogs.GetByAccountIdAsync(
            accountId, page, pageSize, fromDt, toExclusive, ct);

        return (data.Select(MapToResponse).ToList(), totalCount);
    }

    private static AuditLogResponse MapToResponse(AuditLog log)
    {
        using var document = JsonDocument.Parse(log.Payload);
        return new AuditLogResponse(
            log.Id,
            log.EventId,
            log.Topic,
            log.Partition,
            log.Offset,
            log.ReceivedAt,
            document.RootElement.Clone());
    }
}
