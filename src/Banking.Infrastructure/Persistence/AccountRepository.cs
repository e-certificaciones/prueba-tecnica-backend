using Banking.Domain.Abstractions;
using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence;

public sealed class AccountRepository(BankingDbContext db) : IAccountRepository
{
    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        db.Accounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Account?> GetByNumberAsync(string accountNumber, CancellationToken cancellationToken = default) =>
        db.Accounts
            .Include(account => account.Transactions)
            .SingleOrDefaultAsync(account => account.AccountNumber == accountNumber, cancellationToken);

    public async Task<IReadOnlyList<Account>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await db.Accounts.Where(account => account.CustomerId == customerId).ToListAsync(cancellationToken);

    public Task<bool> ExistsByNumberAsync(string accountNumber, CancellationToken cancellationToken = default) =>
        db.Accounts.AnyAsync(account => account.AccountNumber == accountNumber, cancellationToken);

    public Task<bool> ExistsByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        db.Accounts.AnyAsync(account => account.CustomerId == customerId, cancellationToken);

    public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        return db.SaveChangesAsync(cancellationToken);
    }
}
