using FinanceFlow.Application.Audit;

namespace FinanceFlow.Application.Services.Interfaces;

public interface IAuditService
{
    Task<(IReadOnlyList<AuditLogResponse> Data, int TotalCount)> GetAuditLogsByAccountIdAsync(
        Guid accountId,
        int page,
        int pageSize,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct = default);
}
