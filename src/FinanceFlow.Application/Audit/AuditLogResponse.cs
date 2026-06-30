using System.Text.Json;

namespace FinanceFlow.Application.Audit;

public record AuditLogResponse(
    Guid Id,
    Guid EventId,
    string Topic,
    int Partition,
    long Offset,
    DateTimeOffset ReceivedAt,
    JsonElement Payload);
