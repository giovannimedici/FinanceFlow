using FinanceFlow.Application.Abstractions;
using FinanceFlow.Application.Interfaces;
using FinanceFlow.Infrastructure.Audit.Repositories;
using FinanceFlow.Infrastructure.Data;
using FinanceFlow.Infrastructure.Data.Repositories;
using FinanceFlow.Infrastructure.Messaging;
using FinanceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connStr = configuration
            .GetConnectionString("FinanceFlow") 
            ?? throw new InvalidOperationException(
                "Connection string 'FinanceFlow' not found.");

        services.AddDbContext<FinanceFlowDbContext>(opts =>
            opts.UseNpgsql(connStr)
                .UseSnakeCaseNamingConvention());

        services.AddAuditPersistence(configuration);

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        services.AddHostedService<KafkaTopicInitializer>();

        return services;
    }
}