public record AccountResponse(
    Guid Id,
    string OwnerName,
    string DocumentNumber,
    decimal Balance,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);