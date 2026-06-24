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
        services.AddValidatorsFromAssemblyContaining<CreateAccountRequestValidator>();

        return services;
    }
}