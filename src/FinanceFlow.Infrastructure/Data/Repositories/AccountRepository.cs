using FinanceFlow.Application.Interfaces;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Data.Repositories;

public class AccountRepository(FinanceFlowDbContext db) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(
        Guid id,
        CancellationToken ct) =>
        db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Account?> GetByDocumentAsync(
        string doc,
        CancellationToken ct) =>
        db.Accounts.FirstOrDefaultAsync(a => a.DocumentNumber == doc, ct);

    public async Task<IReadOnlyList<Account>> GetAllAsync(
        AccountStatus? status,
        CancellationToken ct)
    {
        var query = db.Accounts.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(
        Account account,
        CancellationToken ct)
    {
        await db.Accounts.AddAsync(account, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(
        Account account,
        CancellationToken ct)
    {
        db.Accounts.Update(account);
        await db.SaveChangesAsync(ct);
    }
}