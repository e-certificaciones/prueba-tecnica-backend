using Banking.Application.Accounts;
using Banking.Application.Customers;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAccountService, AccountService>();
        return services;
    }
}
