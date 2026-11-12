using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Id).ValueGeneratedNever();
        builder.Property(transaction => transaction.Type).IsRequired();
        builder.Property(transaction => transaction.Amount).HasPrecision(18, 2);
        builder.Property(transaction => transaction.OccurredAt).IsRequired();
        builder.Property(transaction => transaction.ResultingBalance).HasPrecision(18, 2);
        builder.Property(transaction => transaction.CounterpartyAccountNumber).HasMaxLength(17);
    }
}
