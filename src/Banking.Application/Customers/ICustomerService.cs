using Banking.Application.Contracts;

namespace Banking.Application.Customers;

public interface ICustomerService
{
    Task<CustomerResponse> Create(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerResponse>> List(CancellationToken cancellationToken = default);

    Task<CustomerResponse> Get(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerResponse> Update(Guid id, CreateCustomerRequest request, CancellationToken cancellationToken = default);
}
