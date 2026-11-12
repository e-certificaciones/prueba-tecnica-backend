using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

public sealed class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public decimal ResultingBalance { get; private set; }
    public string? CounterpartyAccountNumber { get; private set; }

    private Transaction()
    {
    }

    internal Transaction(
        Guid accountId,
        TransactionType type,
        decimal amount,
        DateTime occurredAt,
        decimal resultingBalance,
        string? counterpartyAccountNumber = null)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Type = type;
        Amount = amount;
        OccurredAt = occurredAt;
        ResultingBalance = resultingBalance;
        CounterpartyAccountNumber = counterpartyAccountNumber;
    }
}
