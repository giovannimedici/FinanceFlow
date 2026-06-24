using FinanceFlow.Application.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts")
            .WithTags("Accounts");

        // POST /api/accounts
        group.MapPost("/", async (
            CreateAccountRequest request,
            IValidator<CreateAccountRequest> validator,
            IAccountService service,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var account = await service.CreateAsync(request, ct);
            return Results.Created($"/api/accounts/{account.Id}", account);
        })
        .WithSummary("Create a new account")
        .Produces<AccountResponse>(201)
        .ProducesValidationProblem();

        // GET /api/accounts/{id}
        group.MapGet("/{id:guid}", async (Guid id, IAccountService service, CancellationToken ct) =>
        {
            var account = await service.GetByIdAsync(id, ct);
            return account is null
                ? Results.NotFound(new ProblemDetails { Title = "Account not found", Status = 404 })
                : Results.Ok(account);
        })
        .WithSummary("Search an account by Id")
        .Produces<AccountResponse>()
        .Produces<ProblemDetails>(404);

        // GET /api/accounts?status=Active
        group.MapGet("/", async (string? status, IAccountService service, CancellationToken ct) =>
        {
            var accounts = await service.GetAllAsync(status, ct);
            return Results.Ok(accounts);
        })
        .WithSummary("List all accounts, optionally filtered by status")
        .Produces<IEnumerable<AccountResponse>>();

        // PATCH /api/accounts/{id}/status
        group.MapPatch("/{id:guid}/status", async (
            Guid id,
            UpdateAccountStatusRequest request,
            IAccountService service,
            CancellationToken ct) =>
        {
            var account = await service.UpdateStatusAsync(id, request, ct);
            return Results.Ok(account);
        })
        .WithSummary("Update account status")
        .Produces<AccountResponse>()
        .Produces<ProblemDetails>(404)
        .Produces<ProblemDetails>(422);
    }
}