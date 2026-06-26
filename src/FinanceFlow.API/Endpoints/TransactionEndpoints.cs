using FinanceFlow.Application.Services.Interfaces;
using FluentValidation;

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/accounts/{accountId:guid}/deposit", async (Guid accountId, DepositRequest request, ITransactionService transactionService, CancellationToken ct) =>
        {
            var response = await transactionService.DepositAsync(accountId, request, ct);
            return Results.Ok(response);
        })
        .WithSummary("Deposit an amount into the specified account")
        .WithName("Deposit")
        .WithTags("Transactions");

        app.MapPost("/api/accounts/{accountId:guid}/withdraw", async (Guid accountId, WithdrawRequest request, ITransactionService transactionService, CancellationToken ct) =>
        {
            var response = await transactionService.WithdrawAsync(accountId, request, ct);
            return Results.Ok(response);
        })
        .WithSummary("Withdraw an amount from the specified account")
        .WithName("Withdraw")
        .WithTags("Transactions");

        app.MapGet("/api/accounts/{accountId:guid}/transactions", async (Guid accountId, int page, int pageSize, ITransactionService transactionService, CancellationToken ct) =>
        {
            var (data, totalCount) = await transactionService.GetTransactionsByAccountIdAsync(accountId, page, pageSize, ct);
            return Results.Ok(new { Data = data, TotalCount = totalCount });
        })
        .WithSummary("Get transactions for the specified account with pagination")
        .WithName("GetTransactionsByAccountId")
        .WithTags("Transactions");

        app.MapPost("/api/transfers", async (
        TransferRequest request,
        IValidator<TransferRequest> validator,
        ITransferService transferService,
        CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await transferService.TransferAsync(request, ct);
            return Results.Ok(result);
        })
        .WithSummary("Transfer an amount from one account to another")
        .WithName("Transfer between accounts")
        .WithTags("Transactions");
    }
}