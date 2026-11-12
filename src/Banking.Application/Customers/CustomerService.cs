using Banking.Application.Contracts;
using Banking.Domain.Abstractions;
using Banking.Domain.Entities;
using Banking.Domain.Exceptions;

namespace Banking.Application.Customers;

public sealed class CustomerService(ICustomerRepository customers, IClock clock) : ICustomerService
{
    private const int MaxNameLength = 150;

    public async Task<CustomerResponse> Create(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var fullName = Validate(request);

        var customer = new Customer(
            fullName,
            request.DateOfBirth,
            ContractMapping.ToDomain(request.Gender),
            request.MonthlyIncome,
            clock.UtcNow);

        await customers.AddAsync(customer, cancellationToken);
        return ToResponse(customer);
    }

    public async Task<IReadOnlyList<CustomerResponse>> List(CancellationToken cancellationToken = default)
    {
        var listed = await customers.ListAsync(cancellationToken);
        return listed
            .OrderBy(customer => customer.CreatedAt)
            .ThenBy(customer => customer.FullName, StringComparer.Ordinal)
            .Select(ToResponse)
            .ToArray();
    }

    public async Task<CustomerResponse> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
            throw new CustomerNotFoundException(id);

        return ToResponse(customer);
    }

    public async Task<CustomerResponse> Update(Guid id, CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
            throw new CustomerNotFoundException(id);

        var fullName = Validate(request);
        customer.Update(
            fullName,
            request.DateOfBirth,
            ContractMapping.ToDomain(request.Gender),
            request.MonthlyIncome);
        await customers.UpdateAsync(customer, cancellationToken);
        return ToResponse(customer);
    }

    private string Validate(CreateCustomerRequest request)
    {
        var fullName = request.FullName?.Trim() ?? string.Empty;
        if (fullName.Length == 0)
            throw new BusinessException("El nombre no puede estar vacío.");

        if (fullName.Length > MaxNameLength)
            throw new BusinessException("El nombre no puede superar 150 caracteres.");

        var today = DateOnly.FromDateTime(clock.UtcNow);
        if (request.DateOfBirth > today)
            throw new BusinessException("La fecha de nacimiento no puede ser futura.");

        if (!Enum.IsDefined(request.Gender))
            throw new BusinessException("El género no es válido.");

        if (request.MonthlyIncome < 0)
            throw new InvalidAmountException("Los ingresos mensuales no pueden ser negativos.");

        return fullName;
    }

    private static CustomerResponse ToResponse(Customer customer) =>
        new(
            customer.Id,
            customer.FullName,
            customer.DateOfBirth,
            ContractMapping.ToOption(customer.Gender),
            customer.MonthlyIncome,
            customer.CreatedAt);
}
