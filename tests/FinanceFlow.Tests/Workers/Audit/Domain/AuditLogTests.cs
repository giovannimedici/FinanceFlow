using FinanceFlow.Workers.Audit.Domain;
using FluentAssertions;

namespace FinanceFlow.Tests.Workers.Audit.Domain;

public class AuditLogTests
{
    [Fact]
    public void Create_WithValidData_ReturnsAuditLogWithGeneratedIdAndTimestamp()
    {
        var eventId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        const string topic = "finance.transactions.created";
        const int partition = 2;
        const long offset = 100;
        const string payload = """{"EventId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}""";

        var log = AuditLog.Create(eventId, topic, partition, offset, payload);

        log.Id.Should().NotBeEmpty();
        log.EventId.Should().Be(eventId);
        log.Topic.Should().Be(topic);
        log.Partition.Should().Be(partition);
        log.Offset.Should().Be(offset);
        log.Payload.Should().Be(payload);
        log.ReceivedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_GeneratesUniqueIdsForEachCall()
    {
        var first = AuditLog.Create(Guid.NewGuid(), "topic", 0, 1, "{}");
        var second = AuditLog.Create(Guid.NewGuid(), "topic", 0, 2, "{}");

        first.Id.Should().NotBe(second.Id);
    }
}
