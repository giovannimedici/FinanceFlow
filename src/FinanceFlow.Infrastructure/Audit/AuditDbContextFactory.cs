using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinanceFlow.Infrastructure.Audit;

public class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuditDbContext>();

        var connStr =
            "Host=localhost;Port=5432;Database=financeflow;Username=postgres;Password=postgres";

        optionsBuilder
            .UseNpgsql(connStr)
            .UseSnakeCaseNamingConvention();

        return new AuditDbContext(optionsBuilder.Options);
    }
}
