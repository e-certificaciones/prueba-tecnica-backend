using Banking.Domain.Enums;
using Banking.Domain.Exceptions;

namespace Banking.Domain.Entities;

public sealed class Account
{
    private readonly List<Transaction> _transactions = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyList<Transaction> Transactions => _transactions;

    private Account()
    {
    }

    public static Account Open(Guid customerId, string accountNumber, decimal initialBalance, DateTime createdAt)
    {
        if (initialBalance < 0)
            throw new InvalidAmountException("El saldo inicial no puede ser negativo.");

        var account = new Account
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            AccountNumber = accountNumber,
            Balance = 0m,
            CreatedAt = createdAt
        };

        if (initialBalance > 0)
            account.Deposit(initialBalance, createdAt);

        return account;
    }

    public Transaction Deposit(decimal amount, DateTime occurredAt)
    {
        EnsurePositive(amount);
        Balance += amount;
        return AddTransaction(TransactionType.Deposit, amount, occurredAt);
    }

    public Transaction Withdraw(decimal amount, DateTime occurredAt)
    {
        EnsurePositive(amount);

        if (amount > Balance)
            throw new InsufficientFundsException();

        Balance -= amount;
        return AddTransaction(TransactionType.Withdrawal, amount, occurredAt);
    }

    public Transaction SendTransfer(decimal amount, DateTime occurredAt, string counterpartyAccountNumber)
    {
        EnsurePositive(amount);

        if (amount > Balance)
            throw new InsufficientFundsException();

        Balance -= amount;
        return AddTransaction(TransactionType.TransferSent, amount, occurredAt, counterpartyAccountNumber);
    }

    public Transaction ReceiveTransfer(decimal amount, DateTime occurredAt, string counterpartyAccountNumber)
    {
        EnsurePositive(amount);
        Balance += amount;
        return AddTransaction(TransactionType.TransferReceived, amount, occurredAt, counterpartyAccountNumber);
    }

    private Transaction AddTransaction(
        TransactionType type,
        decimal amount,
        DateTime occurredAt,
        string? counterpartyAccountNumber = null)
    {
        var transaction = new Transaction(Id, type, amount, occurredAt, Balance, counterpartyAccountNumber);
        _transactions.Add(transaction);
        return transaction;
    }

    private static void EnsurePositive(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidAmountException("El monto debe ser mayor que cero.");
    }
}
