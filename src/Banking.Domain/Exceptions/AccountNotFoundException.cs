namespace Banking.Domain.Exceptions;

public sealed class AccountNotFoundException : BusinessException
{
    public AccountNotFoundException(string accountNumber)
        : base($"No existe la cuenta {accountNumber}.")
    {
        AccountNumber = accountNumber;
    }

    public string AccountNumber { get; }
}
