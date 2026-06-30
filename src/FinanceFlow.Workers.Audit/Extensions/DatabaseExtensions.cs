using FinanceFlow.Infrastructure;
using FinanceFlow.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Workers.Audit.Extensions;

public static class DatabaseExtensions
{
    public static async Task ApplyMigrationsAsync(this IHost app)
    {
        if (!app.Services.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
    }
}
