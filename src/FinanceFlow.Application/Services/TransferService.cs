using FinanceFlow.Application.Abstractions;
using FinanceFlow.Application.Interfaces;
using FinanceFlow.Application.Services.Interfaces;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace FinanceFlow.Application.Services;

public class TransferService : ITransferService
{
    private readonly ITransactionRepository _transactions;
    private readonly IEventPublisher _publisher;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<TransferService> _logger;
    private readonly IAccountRepository _accounts;

    public TransferService(
        ITransactionRepository transactions,
        IEventPublisher publisher,
        IUnitOfWork uow,
        ILogger<TransferService> logger,
        IAccountRepository accounts
        )
    {
        _transactions = transactions;
        _publisher = publisher;
        _uow = uow;
        _logger = logger;
        _accounts = accounts;
    }

    public async Task<TransferResponse> TransferAsync(TransferRequest request, CancellationToken ct)
    {
        var ids = new[] { request.SourceAccountId, request.DestinationAccountId }
                        .OrderBy(id => id)
                        .ToArray();

        var firstId = ids[0];
        var secondId = ids[1];

        await using var dbTransaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            var accounts = await _accounts.SelectForUpdate(firstId, secondId, ct);

            if (!accounts.TryGetValue(request.SourceAccountId, out var source))
                throw new DomainException("Source account not found.");

            if (!accounts.TryGetValue(request.DestinationAccountId, out var destination))
                throw new DomainException("Destination account not found.");

            if (source.Status != AccountStatus.Active)
                throw new DomainException("Source account is not active.");

            if (destination.Status != AccountStatus.Active)
                throw new DomainException("Destination account is not active.");

            source.Withdraw(request.Amount);
            destination.Deposit(request.Amount);

            var transferId = Guid.NewGuid();

            var txOut = Transaction.Create(
                accountId: source.Id,
                type: TransactionType.TransferOut,
                amount: request.Amount,
                balanceAfter: source.Balance,
                relatedAccountId: destination.Id,
                description: request.Description);

            var txIn = Transaction.Create(
                accountId: destination.Id,
                type: TransactionType.TransferIn,
                amount: request.Amount,
                balanceAfter: destination.Balance,
                relatedAccountId: source.Id,
                description: request.Description);

            await _transactions.AddAsync(txOut, ct);
            await _transactions.AddAsync(txIn, ct);
            await _accounts.UpdateAsync(source);
            await _accounts.UpdateAsync(destination);
            await _uow.SaveChangesAsync(ct);

            await dbTransaction.CommitAsync(ct);
            _logger.LogInformation(
                "Transfer committed. {TransferId} from {SourceAccountId} to {DestinationAccountId} amount {Amount}",
                transferId, source.Id, destination.Id, request.Amount);
            
            //await PublishEventAsync(txIn, txOut, source, destination, ct);

            return new TransferResponse(transferId, source.Balance, destination.Balance);
        }
        catch (DomainException ex)
        {
            await dbTransaction.RollbackAsync(ct);
            _logger.LogError("Domain exception {ex} occurred during transfer from {SourceAccountId} to {DestinationAccountId}.", ex.Message, request.SourceAccountId, request.DestinationAccountId);
            throw;
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync(ct);
            _logger.LogError("An unexpected error {ex} occurred during transfer from {SourceAccountId} to {DestinationAccountId}.", ex.Message, request.SourceAccountId, request.DestinationAccountId);
            throw;
        }

    }

    private async Task PublishEventAsync(Transaction txIn, Transaction txOut, Account source, Account destination, CancellationToken ct)
    {
        try
        {
            TransactionCreatedEvent @eventIn = new TransactionCreatedEvent
            (EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                TransactionId: txIn.Id,
                AccountId: txIn.AccountId,
                Type: txIn.Type.ToString(),
                Amount: txIn.Amount,
                BalanceAfter: txIn.BalanceAfter,
                RelatedAccountId: txIn.RelatedAccountId,
                SchemaVersion: 1);

            TransactionCreatedEvent @eventOut = new TransactionCreatedEvent
            (EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                TransactionId: txOut.Id,
                AccountId: txOut.AccountId,
                Type: txOut.Type.ToString(),
                Amount: txOut.Amount,
                BalanceAfter: txOut.BalanceAfter,
                RelatedAccountId: txOut.RelatedAccountId,
                SchemaVersion: 1);

            await _publisher.PublishAsync(
                "finance.transactions.created",
                source.Id.ToString(),
                @eventOut, ct);

            await _publisher.PublishAsync(
                "finance.transactions.created",
                destination.Id.ToString(),
                @eventIn, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish Kafka events for transfer. TransactionIds: {TxOut}, {TxIn}",
                txOut.Id, txIn.Id);
        }
    }
}