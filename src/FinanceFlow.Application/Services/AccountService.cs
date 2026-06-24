using FinanceFlow.Application.Interfaces;
using FinanceFlow.Application.Services.Interfaces;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Exceptions;

namespace FinanceFlow.Application.Services;
public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;

    public AccountService(IAccountRepository repository)
        => _repository = repository;

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken ct)
    {
        var account = Account.Create(request.OwnerName, request.DocumentNumber);
        await _repository.AddAsync(account, ct);
        return MapToResponse(account);
    }

    public async Task<AccountResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var account = await _repository.GetByIdAsync(id, ct);
        return account is null ? null : MapToResponse(account);
    }

    public async Task<IEnumerable<AccountResponse>> GetAllAsync(string? status, CancellationToken ct)
    {
        var accounts = await _repository.GetAllAsync(null, ct);

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<AccountStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            accounts = accounts.Where(a => a.Status == parsedStatus).ToList();
        }

        return accounts.Select(MapToResponse);
    }

    public async Task<AccountResponse> UpdateStatusAsync(Guid id, UpdateAccountStatusRequest request, CancellationToken ct)
    {
        var account = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Account {id} not found");

        if (!Enum.TryParse<AccountStatus>(request.Status, ignoreCase: true, out var newStatus))
            throw new DomainException($"Status '{request.Status}' is not a valid status.");

        switch (newStatus)
        {
            case AccountStatus.Blocked: account.Block(); break;
            case AccountStatus.Active:  account.Activate(); break;
            case AccountStatus.Closed:  account.Close(); break;
        }

        await _repository.UpdateAsync(account, ct);
        return MapToResponse(account);
    }

    private static AccountResponse MapToResponse(Account a) =>
        new(a.Id, a.OwnerName, a.DocumentNumber, a.Balance, a.Status.ToString(), a.CreatedAt, a.UpdatedAt);
}