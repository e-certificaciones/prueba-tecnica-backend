namespace Banking.Domain.Exceptions;

public sealed class AccountNumberUnavailableException : BusinessException
{
    public AccountNumberUnavailableException()
        : base("No se pudo generar un número de cuenta único.")
    {
    }
}
