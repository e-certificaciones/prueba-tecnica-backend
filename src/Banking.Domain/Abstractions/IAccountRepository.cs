using Banking.Domain.Entities;

namespace Banking.Domain.Abstractions;

public interface IAccountRepository
{
    Task AddAsync(Account account, CancellationToken cancellationToken = default);

    Task<Account?> GetByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
}
