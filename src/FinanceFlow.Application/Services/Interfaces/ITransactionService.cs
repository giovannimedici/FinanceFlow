namespace FinanceFlow.Application.Services.Interfaces;

public interface ITransactionService
{
    Task<TransactionResponse> DepositAsync(Guid accountId, DepositRequest request, CancellationToken ct = default);
    Task<TransactionResponse> WithdrawAsync(Guid accountId, WithdrawRequest request, CancellationToken ct = default);
}