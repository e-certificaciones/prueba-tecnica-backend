using Banking.Domain.Abstractions;
using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence;

public sealed class CustomerRepository(BankingDbContext db) : ICustomerRepository
{
    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Customers.SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Customers.ToListAsync(cancellationToken);

    public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);
        return db.SaveChangesAsync(cancellationToken);
    }
}
