namespace Banking.Domain.Exceptions;

public sealed class InvalidAmountException : BusinessException
{
    public InvalidAmountException(string message) : base(message)
    {
    }
}
