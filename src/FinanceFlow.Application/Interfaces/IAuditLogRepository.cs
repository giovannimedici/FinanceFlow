using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLog> Data, int TotalCount)> GetByAccountIdAsync(
        Guid accountId,
        int page,
        int pageSize,
        DateTimeOffset? from,
        DateTimeOffset? toExclusive,
        CancellationToken ct = default);
}
