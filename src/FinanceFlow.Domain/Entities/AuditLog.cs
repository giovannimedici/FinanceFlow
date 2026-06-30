namespace FinanceFlow.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string Topic { get; private set; } = default!;
    public int Partition { get; private set; }
    public long Offset { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public string Payload { get; private set; } = default!;

    public static AuditLog Create(Guid eventId, string topic, int partition, long offset, string payload)
        => new()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Topic = topic,
            Partition = partition,
            Offset = offset,
            ReceivedAt = DateTimeOffset.UtcNow,
            Payload = payload
        };
}
