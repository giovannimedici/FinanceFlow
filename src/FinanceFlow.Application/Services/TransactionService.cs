using FinanceFlow.Application.Abstractions;
using FinanceFlow.Application.Events;
using FinanceFlow.Application.Interfaces;
using FinanceFlow.Application.Services.Interfaces;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace FinanceFlow.Application.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;
    private readonly IEventPublisher _publisher;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IAccountRepository accounts,
        ITransactionRepository transactions,
        IEventPublisher publisher,
        IUnitOfWork uow,
        ILogger<TransactionService> logger)
    {
        _accounts = accounts;
        _transactions = transactions;
        _publisher = publisher;
        _uow = uow;
        _logger = logger;
    }

    public async Task<TransactionResponse> DepositAsync(
        Guid accountId, DepositRequest request, CancellationToken ct = default)
    {
        var account = await _accounts.GetByIdAsync(accountId, ct)
            ?? throw new NotFoundException($"Account {accountId} not found.");

        await using var dbTx = await _uow.BeginTransactionAsync(ct);
        try
        {
            account.Deposit(request.Amount);

            var transaction = Transaction.Create(
                accountId: accountId,
                type: TransactionType.Deposit,
                amount: request.Amount,
                balanceAfter: account.Balance,
                description: request.Description);

            await _transactions.AddAsync(transaction, ct);
            await _accounts.UpdateAsync(account, ct);
            await _uow.SaveChangesAsync(ct);

            await dbTx.CommitAsync(ct);

            _logger.LogInformation(
                "Deposit committed. {TransactionId} {AccountId} {Amount}",
                transaction.Id, accountId, request.Amount);

            await PublishEventAsync(transaction, ct);

            return new TransactionResponse(transaction.Id, transaction.Amount, account.Balance);
        }
        catch (DomainException)
        {
            await dbTx.RollbackAsync(ct);
            throw;
        }
        catch (Exception ex)
        {
            await dbTx.RollbackAsync(ct);
            _logger.LogError(ex, "Unexpected error during deposit for account {AccountId}", accountId);
            throw;
        }
    }

    public async Task<TransactionResponse> WithdrawAsync(
        Guid accountId, WithdrawRequest request, CancellationToken ct = default)
    {
        var account = await _accounts.GetByIdAsync(accountId, ct)
            ?? throw new NotFoundException($"Account {accountId} not found.");

        await using var dbTx = await _uow.BeginTransactionAsync(ct);
        try
        {
            account.Withdraw(request.Amount); // DomainException se saldo insuficiente ou conta inativa

            var transaction = Transaction.Create(
                accountId: accountId,
                type: TransactionType.Withdrawal,
                amount: request.Amount,
                balanceAfter: account.Balance,
                description: request.Description);

            await _transactions.AddAsync(transaction, ct);
            await _accounts.UpdateAsync(account, ct);
            await _uow.SaveChangesAsync(ct);
            await dbTx.CommitAsync(ct);

            _logger.LogInformation(
                "Withdrawal committed. {TransactionId} {AccountId} {Amount}",
                transaction.Id, accountId, request.Amount);

            await PublishEventAsync(transaction, ct);

            return new TransactionResponse(transaction.Id, transaction.Amount, account.Balance);
        }
        catch (DomainException)
        {
            await dbTx.RollbackAsync(ct);
            throw;
        }
        catch (Exception ex)
        {
            await dbTx.RollbackAsync(ct);
            _logger.LogError(ex, "Unexpected error during withdrawal for account {AccountId}", accountId);
            throw;
        }
    }

    public async Task<(IReadOnlyList<TransactionResponse> Data, int TotalCount)> GetTransactionsByAccountIdAsync(
        Guid accountId, int page, int pageSize, CancellationToken ct = default)
    {
        var account = await _accounts.GetByIdAsync(accountId, ct)
            ?? throw new NotFoundException($"Account {accountId} not found.");

        var transactions = await _transactions.GetByAccountIdAsync(accountId, page, pageSize, ct);

        return (transactions.Data.Select(MapToResponse).ToList(), transactions.TotalCount);
    }
    private async Task PublishEventAsync(Transaction transaction, CancellationToken ct)
    {
        try
        {
            var @event = new TransactionCreatedEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                TransactionId: transaction.Id,
                AccountId: transaction.AccountId,
                Type: transaction.Type.ToString(),
                Amount: transaction.Amount,
                BalanceAfter: transaction.BalanceAfter,
                RelatedAccountId: transaction.RelatedAccountId,
                SchemaVersion: 1);

            await _publisher.PublishAsync(
                topic: KafkaTopics.TransactionsCreated,
                key: transaction.AccountId.ToString(),
                payload: @event,
                ct: ct);

            _logger.LogInformation(
                "Event published. {TransactionId} {AccountId}",
                transaction.Id, transaction.AccountId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish Kafka event for transaction {TransactionId}. DB commit already succeeded.",
                transaction.Id);
        }
    }
    private static TransactionResponse MapToResponse(Transaction t) =>
        new(t.Id, t.Amount, t.BalanceAfter);
}