using Banking.Domain.Abstractions;

namespace Banking.Infrastructure.AccountNumbers;

public sealed class RandomAccountNumberDigits : IAccountNumberDigits
{
    public int Next() => Random.Shared.Next(0, 10000);
}
