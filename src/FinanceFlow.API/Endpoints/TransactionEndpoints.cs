using FinanceFlow.Application.Services.Interfaces;

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/accounts/{accountId:guid}/deposit", async (Guid accountId, DepositRequest request, ITransactionService transactionService, CancellationToken ct) =>
        {
            var response = await transactionService.DepositAsync(accountId, request, ct);
            return Results.Ok(response);
        })
        .WithName("Deposit")
        .WithTags("Transactions");

        app.MapPost("/api/accounts/{accountId:guid}/withdraw", async (Guid accountId, WithdrawRequest request, ITransactionService transactionService, CancellationToken ct) =>
        {
            var response = await transactionService.WithdrawAsync(accountId, request, ct);
            return Results.Ok(response);
        })
        .WithName("Withdraw")
        .WithTags("Transactions");
    }
}