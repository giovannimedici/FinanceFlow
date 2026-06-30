using FinanceFlow.Tests.Workers.Audit.Integration;
using FinanceFlow.Workers.Audit.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace FinanceFlow.Tests.Workers.Audit.Extensions;

[Collection(nameof(AuditPostgreSqlCollection))]
public sealed class DatabaseExtensionsTests : IAsyncLifetime
{
    private readonly AuditPostgreSqlFixture _fixture;

    public DatabaseExtensionsTests(AuditPostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ApplyMigrationsAsync_WhenDevelopment_AppliesPendingMigrations()
    {
        using var host = BuildHost(Environments.Development, _fixture.ConnectionString);

        var act = () => host.ApplyMigrationsAsync();

        await act.Should().NotThrowAsync();

        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        (await db.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await db.Database.CanConnectAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task ApplyMigrationsAsync_WhenNotDevelopment_SkipsMigration()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await container.StartAsync();
        var connectionString = container.GetConnectionString();

        using var host = BuildHost(Environments.Production, connectionString);
        await host.ApplyMigrationsAsync();

        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        var pending = await db.Database.GetPendingMigrationsAsync();
        pending.Should().NotBeEmpty();

        var tableExists = await db.Database
            .SqlQueryRaw<bool>(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_name = 'audit_logs'
                ) AS "Value"
                """)
            .SingleAsync();

        tableExists.Should().BeFalse();
    }

    private static IHost BuildHost(string environmentName, string connectionString)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Environment.EnvironmentName = environmentName;
        builder.Services.AddDbContext<AuditDbContext>(opt =>
            opt.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        return builder.Build();
    }
}
