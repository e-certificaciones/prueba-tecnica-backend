using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Banking.Infrastructure.Persistence;

public sealed class BankingDbContextFactory : IDesignTimeDbContextFactory<BankingDbContext>
{
    public BankingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite(
                "Data Source=banking.db",
                sqlite => sqlite.MigrationsAssembly(typeof(BankingDbContext).Assembly.GetName().Name))
            .Options;

        return new BankingDbContext(options);
    }
}
