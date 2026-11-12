namespace Banking.Domain.Exceptions;

public sealed class InsufficientFundsException : BusinessException
{
    public InsufficientFundsException()
        : base("Fondos insuficientes.")
    {
    }
}
