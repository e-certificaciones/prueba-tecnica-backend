using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;

namespace Banking.UnitTests;

/// <summary>
/// Saldo de la cuenta: depósito, retiro con fondos, retiro sin fondos
/// y el movimiento que queda después de cada operación.
/// </summary>
public class AccountBalanceTests
{
    // Fecha fija para poder comprobar la marca de tiempo del movimiento.
    private static readonly DateTime CreatedAt = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = CreatedAt.AddMinutes(5);

    /// <summary>
    /// Una cuenta abierta en cero no tiene movimientos: el historial nace vacío.
    /// </summary>
    [Fact]
    public void Open_WithZeroBalance_StartsWithoutTransactions()
    {
        var account = Open(0m);

        Assert.Equal(0m, account.Balance);
        Assert.Empty(account.Transactions);
    }

    /// <summary>
    /// Un saldo inicial mayor que cero se guarda como el primer depósito,
    /// con su monto, su fecha y el saldo posterior.
    /// </summary>
    [Fact]
    public void Open_WithPositiveBalance_RecordsOpeningDeposit()
    {
        var account = Open(80m);

        Assert.Equal(80m, account.Balance);
        var transaction = Assert.Single(account.Transactions);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(80m, transaction.Amount);
        Assert.Equal(80m, transaction.ResultingBalance);
        Assert.Equal(CreatedAt, transaction.OccurredAt);
    }

    /// <summary>
    /// Un saldo inicial negativo se rechaza y la cuenta no se crea.
    /// </summary>
    [Fact]
    public void Open_WithNegativeBalance_IsRejected()
    {
        Assert.Throws<InvalidAmountException>(() => Open(-0.01m));
    }

    /// <summary>
    /// Un depósito aumenta el saldo. El movimiento guarda el monto,
    /// la fecha y el saldo inmediatamente posterior.
    /// </summary>
    [Fact]
    public void Deposit_IncreasesBalanceAndRecordsResultingBalance()
    {
        var account = Open(100m);

        var transaction = account.Deposit(25.50m, Later);

        Assert.Equal(125.50m, account.Balance);
        Assert.Equal(125.50m, transaction.ResultingBalance);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(25.50m, transaction.Amount);
        Assert.Equal(Later, transaction.OccurredAt);
    }

    /// <summary>
    /// Un retiro con fondos disminuye el saldo y el movimiento guarda el saldo posterior.
    /// </summary>
    [Fact]
    public void Withdraw_WithFunds_DecreasesBalanceAndRecordsResultingBalance()
    {
        var account = Open(100m);

        var transaction = account.Withdraw(40m, Later);

        Assert.Equal(60m, account.Balance);
        Assert.Equal(60m, transaction.ResultingBalance);
        Assert.Equal(TransactionType.Withdrawal, transaction.Type);
        Assert.Equal(40m, transaction.Amount);
    }

    /// <summary>
    /// Si el monto supera el saldo, el retiro se rechaza con fondos insuficientes.
    /// El saldo y el historial quedan como estaban.
    /// </summary>
    [Fact]
    public void Withdraw_WithoutFunds_ThrowsAndKeepsBalance()
    {
        var account = Open(100m);

        Assert.Throws<InsufficientFundsException>(() => account.Withdraw(100.01m, Later));

        Assert.Equal(100m, account.Balance);
        Assert.Single(account.Transactions);
    }

    /// <summary>
    /// Cero y un monto negativo no son un depósito válido.
    /// El saldo y el historial no cambian.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Deposit_RejectsNonPositiveAmount(decimal amount)
    {
        var account = Open(50m);

        Assert.Throws<InvalidAmountException>(() => account.Deposit(amount, Later));
        Assert.Equal(50m, account.Balance);
        Assert.Single(account.Transactions);
    }

    /// <summary>
    /// Cero y un monto negativo no son un retiro válido.
    /// El saldo y el historial no cambian.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Withdraw_RejectsNonPositiveAmount(decimal amount)
    {
        var account = Open(50m);

        Assert.Throws<InvalidAmountException>(() => account.Withdraw(amount, Later));
        Assert.Equal(50m, account.Balance);
        Assert.Single(account.Transactions);
    }

    // Abre una cuenta de prueba con número fijo y la fecha CreatedAt.
    private static Account Open(decimal initialBalance) =>
        Account.Open(Guid.NewGuid(), "ACC-20261111-0001", initialBalance, CreatedAt);
}
