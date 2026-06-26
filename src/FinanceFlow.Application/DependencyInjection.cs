using FinanceFlow.Application.Services;
using FinanceFlow.Application.Services.Interfaces;
using FinanceFlow.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;


namespace FinanceFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddValidatorsFromAssemblyContaining<CreateAccountRequestValidator>();

        return services;
    }
}