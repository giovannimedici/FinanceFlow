using FinanceFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.API.Extensions;

public static class DatabaseExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<FinanceFlowDbContext>();

        await db.Database.MigrateAsync();
    }
}