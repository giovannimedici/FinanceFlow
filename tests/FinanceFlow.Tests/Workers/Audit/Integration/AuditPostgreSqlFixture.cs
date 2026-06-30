using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FinanceFlow.Tests.Workers.Audit.Integration;

public sealed class AuditPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AuditDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AuditDbContext(options);
    }
}

[CollectionDefinition(nameof(AuditPostgreSqlCollection))]
public sealed class AuditPostgreSqlCollection : ICollectionFixture<AuditPostgreSqlFixture>;
