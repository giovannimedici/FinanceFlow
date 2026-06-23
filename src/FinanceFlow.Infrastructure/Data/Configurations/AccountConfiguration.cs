using FinanceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceFlow.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.OwnerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.DocumentNumber)
            .IsRequired()
            .HasMaxLength(14);

        builder.Property(a => a.Balance)
            .HasPrecision(18, 2);

        builder.Property(a => a.Status)
            .HasConversion<string>() 
            .HasMaxLength(20);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();
            
        builder.HasIndex(a => a.DocumentNumber)
            .IsUnique();
    }
}