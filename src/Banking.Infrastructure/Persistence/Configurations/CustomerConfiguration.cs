using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedNever();
        builder.Property(customer => customer.FullName).IsRequired().HasMaxLength(150);
        builder.Property(customer => customer.DateOfBirth).IsRequired();
        builder.Property(customer => customer.Gender).IsRequired();
        builder.Property(customer => customer.MonthlyIncome).HasPrecision(18, 2);
        builder.Property(customer => customer.CreatedAt).IsRequired();
    }
}
