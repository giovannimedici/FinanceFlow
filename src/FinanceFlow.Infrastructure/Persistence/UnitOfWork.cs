using FinanceFlow.Application.Abstractions;
using FinanceFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinanceFlow.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly FinanceFlowDbContext _context;

    public UnitOfWork(FinanceFlowDbContext context)
    {
        _context = context;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => _context.Database.BeginTransactionAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}