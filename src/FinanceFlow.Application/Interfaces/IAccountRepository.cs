using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<Account?> GetByDocumentAsync(
        string documentNumber,
        CancellationToken ct = default);

    Task<IReadOnlyList<Account>> GetAllAsync(
        AccountStatus? status = null,
        CancellationToken ct = default);

    Task AddAsync(
        Account account,
        CancellationToken ct = default);

    Task UpdateAsync(
        Account account,
        CancellationToken ct = default);
}
