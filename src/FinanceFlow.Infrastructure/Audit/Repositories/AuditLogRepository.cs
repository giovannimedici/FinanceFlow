using System.Text.Json;
using FinanceFlow.Application.Interfaces;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Audit.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AuditDbContext _db;

    public AuditLogRepository(AuditDbContext db) => _db = db;

    public async Task<(IReadOnlyList<AuditLog> Data, int TotalCount)> GetByAccountIdAsync(
        Guid accountId,
        int page,
        int pageSize,
        DateTimeOffset? from,
        DateTimeOffset? toExclusive,
        CancellationToken ct = default)
    {
        var accountFilter = JsonSerializer.Serialize(new { AccountId = accountId });

        IQueryable<AuditLog> query = _db.AuditLogs
            .Where(a => EF.Functions.JsonContains(a.Payload, accountFilter));

        if (from.HasValue)
            query = query.Where(a => a.ReceivedAt >= from.Value);

        if (toExclusive.HasValue)
            query = query.Where(a => a.ReceivedAt < toExclusive.Value);

        query = query.OrderByDescending(a => a.ReceivedAt);

        var total = await query.CountAsync(ct);
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (data, total);
    }
}
