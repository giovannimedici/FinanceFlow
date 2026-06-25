using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Data.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.BalanceAfter).HasPrecision(18, 2);
        builder.Property(t => t.Type).HasConversion<string>();

        builder.HasOne<Account>()
               .WithMany()
               .HasForeignKey(t => t.AccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.AccountId, t.CreatedAt })
               .IsDescending(false, true);  
    }
}