using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinanceFlow.Infrastructure.Data;

public class FinanceFlowDbContextFactory
    : IDesignTimeDbContextFactory<FinanceFlowDbContext>
{
    public FinanceFlowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<FinanceFlowDbContext>();

        var connStr =
            "Host=localhost;Port=5432;Database=financeflow;Username=postgres;Password=postgres";

        optionsBuilder
            .UseNpgsql(connStr)
            .UseSnakeCaseNamingConvention();

        return new FinanceFlowDbContext(optionsBuilder.Options);
    }
}