using FinanceFlow.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceFlow.Infrastructure;

public static class AuditDependencyInjection
{
    public static IServiceCollection AddAuditPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("FinanceFlow")
            ?? throw new InvalidOperationException("Connection string 'FinanceFlow' not found.");

        services.AddDbContext<AuditDbContext>(opts =>
            opts.UseNpgsql(connStr)
                .UseSnakeCaseNamingConvention());

        return services;
    }
}
