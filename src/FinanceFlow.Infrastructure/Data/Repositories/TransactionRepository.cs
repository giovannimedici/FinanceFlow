using FinanceFlow.Application.Interfaces;
using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Data.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly FinanceFlowDbContext _db;

    public TransactionRepository(FinanceFlowDbContext db) => _db = db;

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        await _db.Transactions.AddAsync(transaction, ct);
    }

    public async Task<(IReadOnlyList<Transaction> Data, int TotalCount)> GetByAccountIdAsync(
        Guid accountId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync(ct);
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (data, total);
    }
}