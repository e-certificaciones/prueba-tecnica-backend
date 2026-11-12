using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).ValueGeneratedNever();
        builder.Property(account => account.AccountNumber).IsRequired().HasMaxLength(17);
        builder.HasIndex(account => account.AccountNumber).IsUnique(); // restriccion de campo unique para account number que pide la prueba tecnica
        builder.Property(account => account.Balance).HasPrecision(18, 2);
        builder.Property(account => account.CreatedAt).IsRequired();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(account => account.CustomerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(account => account.Transactions)
            .WithOne()
            .HasForeignKey(transaction => transaction.AccountId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(account => account.Transactions)
            .HasField("_transactions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
