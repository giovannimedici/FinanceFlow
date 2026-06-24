namespace FinanceFlow.Application.Services.Interfaces;

public interface IAccountService
{
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken ct);
    Task<AccountResponse?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<AccountResponse>> GetAllAsync(string? status, CancellationToken ct);
    Task<AccountResponse> UpdateStatusAsync(Guid id, UpdateAccountStatusRequest request, CancellationToken ct);
}