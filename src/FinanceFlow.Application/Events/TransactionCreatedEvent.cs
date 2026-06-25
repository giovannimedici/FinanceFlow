public record TransactionCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TransactionId,
    Guid AccountId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    Guid? RelatedAccountId,
    int SchemaVersion = 1);