namespace Banking.Domain.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
