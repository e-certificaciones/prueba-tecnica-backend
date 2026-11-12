namespace Banking.Domain.Exceptions;

public sealed class CustomerAccountNotFoundException : BusinessException
{
    public CustomerAccountNotFoundException(Guid customerId)
        : base($"El cliente {customerId} no tiene una cuenta.")
    {
        CustomerId = customerId;
    }

    public Guid CustomerId { get; }
}
