using FinanceFlow.Application.Interfaces;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        AccountStatus? status = null,
        CancellationToken ct = default)
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

    public async Task<Dictionary<Guid, Account>> SelectForUpdate(Guid firstId, Guid secondId, CancellationToken ct)
    {
        return await db.Accounts
                        .FromSqlRaw(
                            "SELECT * FROM accounts WHERE id = ANY(@ids) FOR UPDATE",
                            new NpgsqlParameter("ids", new[] { firstId, secondId }))
                            .ToDictionaryAsync(a => a.Id, ct);
    }
}