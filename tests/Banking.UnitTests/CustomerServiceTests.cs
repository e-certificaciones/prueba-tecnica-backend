using Banking.Application.Contracts;
using Banking.Application.Customers;
using Banking.Domain.Abstractions;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;

namespace Banking.UnitTests;

/// <summary>
/// Clientes: alta, consulta, listado y edición.
/// </summary>
public class CustomerServiceTests
{
    private static readonly DateTime Today = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Un nombre vacío o en blanco se rechaza y no se guarda el cliente.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_RejectsBlankName(string fullName)
    {
        var customers = new InMemoryCustomerRepository();
        var service = new CustomerService(customers, new FixedClock(Today));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.Create(Request(fullName)));

        Assert.Equal("El nombre no puede estar vacío.", exception.Message);
        Assert.Empty(customers.Customers);
    }

    /// <summary>
    /// El nombre no puede pasar de 150 caracteres.
    /// </summary>
    [Fact]
    public async Task Create_RejectsNameLongerThan150()
    {
        var service = new CustomerService(new InMemoryCustomerRepository(), new FixedClock(Today));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.Create(Request(new string('a', 151))));

        Assert.Equal("El nombre no puede superar 150 caracteres.", exception.Message);
    }

    /// <summary>
    /// La fecha de nacimiento no puede ser posterior a hoy, según el reloj UTC.
    /// </summary>
    [Fact]
    public async Task Create_RejectsFutureBirthDate()
    {
        var service = new CustomerService(new InMemoryCustomerRepository(), new FixedClock(Today));
        var request = Request("Ana Pérez") with { DateOfBirth = new DateOnly(2026, 11, 12) };

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.Create(request));

        Assert.Equal("La fecha de nacimiento no puede ser futura.", exception.Message);
    }

    /// <summary>
    /// Los ingresos mensuales negativos se rechazan.
    /// </summary>
    [Fact]
    public async Task Create_RejectsNegativeIncome()
    {
        var service = new CustomerService(new InMemoryCustomerRepository(), new FixedClock(Today));
        var request = Request("Ana Pérez") with { MonthlyIncome = -1m };

        await Assert.ThrowsAsync<InvalidAmountException>(() => service.Create(request));
    }

    /// <summary>
    /// Un cliente válido se guarda con el nombre recortado y la fecha del reloj.
    /// </summary>
    [Fact]
    public async Task Create_PersistsCustomer()
    {
        var customers = new InMemoryCustomerRepository();
        var service = new CustomerService(customers, new FixedClock(Today));

        var response = await service.Create(Request("  Ana Pérez  "));

        Assert.Equal("Ana Pérez", response.FullName);
        Assert.Equal(GenderOption.Female, response.Gender);
        Assert.Equal(1500m, response.MonthlyIncome);
        Assert.Equal(Today, response.CreatedAt);
        Assert.Equal(response.Id, Assert.Single(customers.Customers).Id);
    }

    /// <summary>
    /// El listado sale por fecha de registro y, si coinciden, por nombre.
    /// </summary>
    [Fact]
    public async Task List_OrdersByRegistrationThenName()
    {
        var customers = new InMemoryCustomerRepository();
        var ana = new Customer("Ana", new DateOnly(1990, 5, 2), Gender.Female, 1500m, Today);
        var zoe = new Customer("Zoe", new DateOnly(1988, 1, 1), Gender.Female, 900m, Today.AddHours(-1));
        var bea = new Customer("Bea", new DateOnly(1992, 3, 3), Gender.Male, 800m, Today);
        customers.Customers.Add(ana);
        customers.Customers.Add(zoe);
        customers.Customers.Add(bea);
        var service = new CustomerService(customers, new FixedClock(Today));

        var listed = await service.List();

        Assert.Equal([zoe.Id, ana.Id, bea.Id], listed.Select(customer => customer.Id));
    }

    /// <summary>
    /// Consultar un id desconocido no devuelve un perfil.
    /// </summary>
    [Fact]
    public async Task Get_RejectsMissingCustomer()
    {
        var service = new CustomerService(new InMemoryCustomerRepository(), new FixedClock(Today));
        var id = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<CustomerNotFoundException>(() => service.Get(id));

        Assert.Equal(id, exception.CustomerId);
    }

    /// <summary>
    /// La edición cambia el perfil y conserva el id y la fecha de registro.
    /// </summary>
    [Fact]
    public async Task Update_ChangesProfileAndKeepsIdentity()
    {
        var customers = new InMemoryCustomerRepository();
        var service = new CustomerService(customers, new FixedClock(Today));
        var created = await service.Create(Request("Ana Pérez"));

        var updated = await service.Update(created.Id, new CreateCustomerRequest(
            "  Luis Gómez  ",
            new DateOnly(1985, 1, 9),
            GenderOption.Male,
            2200m));

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(Today, updated.CreatedAt);
        Assert.Equal("Luis Gómez", updated.FullName);
        Assert.Equal(new DateOnly(1985, 1, 9), updated.DateOfBirth);
        Assert.Equal(GenderOption.Male, updated.Gender);
        Assert.Equal(2200m, updated.MonthlyIncome);
        Assert.Equal(1, customers.UpdateCalls);
    }

    /// <summary>
    /// No se edita un cliente que no existe y no se intenta guardar.
    /// </summary>
    [Fact]
    public async Task Update_RejectsMissingCustomer()
    {
        var customers = new InMemoryCustomerRepository();
        var service = new CustomerService(customers, new FixedClock(Today));
        var id = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<CustomerNotFoundException>(() => service.Update(id, Request("Ana Pérez")));

        Assert.Equal(id, exception.CustomerId);
        Assert.Equal(0, customers.UpdateCalls);
    }

    private static CreateCustomerRequest Request(string fullName) =>
        new(fullName, new DateOnly(1990, 5, 2), GenderOption.Female, 1500m);

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class InMemoryCustomerRepository : ICustomerRepository
    {
        public List<Customer> Customers { get; } = [];
        public int UpdateCalls { get; private set; }

        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            Customers.Add(customer);
            return Task.CompletedTask;
        }

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Customers.SingleOrDefault(customer => customer.Id == id));

        public Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Customer>>(Customers);

        public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            return Task.CompletedTask;
        }
    }
}
