using Banking.Domain.Abstractions;
using Banking.Infrastructure.AccountNumbers;
using Banking.Infrastructure.Persistence;
using Banking.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=banking.db";

        services.AddDbContext<BankingDbContext>(options =>
            options.UseSqlite(
                connectionString,
                sqlite => sqlite.MigrationsAssembly(typeof(BankingDbContext).Assembly.GetName().Name)));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IAccountNumberDigits, RandomAccountNumberDigits>();

        return services;
    }

    public static void Migrate(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        database.Database.Migrate();
    }
}
