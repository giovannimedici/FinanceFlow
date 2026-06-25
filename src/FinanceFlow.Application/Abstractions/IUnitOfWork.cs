using Microsoft.EntityFrameworkCore.Storage;

namespace FinanceFlow.Application.Abstractions;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}