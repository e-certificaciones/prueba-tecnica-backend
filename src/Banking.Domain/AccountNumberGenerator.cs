using Banking.Domain.Exceptions;

namespace Banking.Domain;

public sealed class AccountNumberGenerator
{
    public string Generate(DateOnly createdOn, int fourDigits)
    {
        if (fourDigits is < 0 or > 9999)
            throw new InvalidAmountException("Los cuatro dígitos deben estar entre 0 y 9999.");

        return $"ACC-{createdOn:yyyyMMdd}-{fourDigits:0000}";
    }
}
