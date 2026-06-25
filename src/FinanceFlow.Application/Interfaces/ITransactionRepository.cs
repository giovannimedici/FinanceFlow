using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Interfaces;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task<(IReadOnlyList<Transaction> Data, int TotalCount)> GetByAccountIdAsync(
        Guid accountId, int page, int pageSize, CancellationToken ct = default);
}