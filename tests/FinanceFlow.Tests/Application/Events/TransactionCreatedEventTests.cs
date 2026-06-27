using System.Text.Json;
using FinanceFlow.Application.Events;
using FluentAssertions;

namespace FinanceFlow.Tests.Application.Events;

public class TransactionCreatedEventTests
{
    [Fact]
    public void SchemaVersion_DefaultsToOne()
    {
        var @event = new TransactionCreatedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            TransactionId: Guid.NewGuid(),
            AccountId: Guid.NewGuid(),
            Type: "Deposit",
            Amount: 100m,
            BalanceAfter: 500m,
            RelatedAccountId: null);

        @event.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void SerializesToJson_WithAllFields()
    {
        var eventId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var transactionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var accountId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var occurredAt = new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);

        var @event = new TransactionCreatedEvent(
            EventId: eventId,
            OccurredAt: occurredAt,
            TransactionId: transactionId,
            AccountId: accountId,
            Type: "Withdrawal",
            Amount: 50.25m,
            BalanceAfter: 449.75m,
            RelatedAccountId: null,
            SchemaVersion: 1);

        var json = JsonSerializer.Serialize(@event);

        json.Should().Contain("\"EventId\":\"11111111-1111-1111-1111-111111111111\"");
        json.Should().Contain("\"TransactionId\":\"22222222-2222-2222-2222-222222222222\"");
        json.Should().Contain("\"AccountId\":\"33333333-3333-3333-3333-333333333333\"");
        json.Should().Contain("\"Type\":\"Withdrawal\"");
        json.Should().Contain("\"Amount\":50.25");
        json.Should().Contain("\"BalanceAfter\":449.75");
        json.Should().Contain("\"SchemaVersion\":1");
    }
}
