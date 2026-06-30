using FinanceFlow.Application.Services.Interfaces;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit/accounts/{accountId:guid}", async (
            Guid accountId,
            IAuditService auditService,
            CancellationToken ct,
            int page = 1,
            int pageSize = 20,
            DateOnly? from = null,
            DateOnly? to = null) =>
        {
            var (data, totalCount) = await auditService.GetAuditLogsByAccountIdAsync(
                accountId, page, pageSize, from, to, ct);

            return Results.Ok(new { Data = data, TotalCount = totalCount });
        })
        .WithSummary("Get paginated audit logs for the specified account")
        .WithName("GetAuditLogsByAccountId")
        .WithTags("Audit");
    }
}
