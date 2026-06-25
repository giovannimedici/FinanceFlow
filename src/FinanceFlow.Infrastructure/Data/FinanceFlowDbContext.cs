using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Data;
public class FinanceFlowDbContext(DbContextOptions<FinanceFlowDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}