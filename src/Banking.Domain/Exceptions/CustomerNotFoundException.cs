namespace Banking.Domain.Exceptions;

public sealed class CustomerNotFoundException : BusinessException
{
    public CustomerNotFoundException(Guid customerId)
        : base($"No existe el cliente {customerId}.")
    {
        CustomerId = customerId;
    }

    public Guid CustomerId { get; }
}
