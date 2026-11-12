using Banking.Application.Accounts;
using Banking.Application.Contracts;
using Banking.Domain.Abstractions;
using Banking.Domain.Entities;
using Banking.Domain.Exceptions;

namespace Banking.UnitTests;

/// <summary>
/// Casos de uso de la cuenta: cliente inexistente, número repetido,
/// cuenta inexistente al depositar e historial ordenado.
/// </summary>
public class AccountServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Abrir una cuenta para un cliente que no existe lanza CustomerNotFoundException
    /// y no genera ni guarda un número.
    /// </summary>
    [Fact]
    public async Task Create_WhenCustomerDoesNotExist_Throws()
    {
        var customerId = Guid.NewGuid();
        var accounts = new InMemoryAccountRepository();
        var digits = new QueueDigits(1);
        var service = Service(new InMemoryCustomerRepository(), accounts, new FixedClock(Now), digits);

        var exception = await Assert.ThrowsAsync<CustomerNotFoundException>(() =>
            service.Create(new CreateAccountRequest(customerId, 0m)));

        Assert.Equal(customerId, exception.CustomerId);
        Assert.Equal(0, digits.Calls);
        Assert.Empty(accounts.Accounts);
    }

    /// <summary>
    /// Si el primer número ya existe, se pide otro sufijo y se guarda una sola vez.
    /// </summary>
    [Fact]
    public async Task Create_WhenFirstNumberExists_UsesTheNextOne()
    {
        var customers = new InMemoryCustomerRepository();
        var customer = await SaveCustomer(customers);
        var accounts = new InMemoryAccountRepository();
        var clock = new FixedClock(Now);
        var taken = Account.Open(Guid.NewGuid(), "ACC-20261111-0007", 0m, Now);
        accounts.Accounts.Add(taken.AccountNumber, taken);
        var service = Service(customers, accounts, clock, new QueueDigits(7, 8));

        var response = await service.Create(new CreateAccountRequest(customer.Id, 25m));

        Assert.Equal("ACC-20261111-0008", response.AccountNumber);
        Assert.Equal(25m, response.Balance);
        Assert.Equal(2, accounts.Accounts.Count);
        var created = accounts.Accounts["ACC-20261111-0008"];
        Assert.Equal(25m, Assert.Single(created.Transactions).ResultingBalance);
    }

    /// <summary>
    /// Tras 10 números ocupados se lanza una excepción de negocio y no se guarda la cuenta.
    /// </summary>
    [Fact]
    public async Task Create_WhenTenNumbersExist_Throws()
    {
        var customers = new InMemoryCustomerRepository();
        var customer = await SaveCustomer(customers);
        var accounts = new AlwaysTakenAccountRepository();
        var digits = new FixedDigits(1234);
        var service = Service(customers, accounts, new FixedClock(Now), digits);

        await Assert.ThrowsAsync<AccountNumberUnavailableException>(() =>
            service.Create(new CreateAccountRequest(customer.Id, 0m)));

        Assert.Equal(10, digits.Calls);
        Assert.Equal(10, accounts.ExistsCalls);
        Assert.Equal(0, accounts.AddCalls);
    }

    /// <summary>
    /// Un cliente que ya tiene cuenta no puede abrir otra, y no se genera un número nuevo.
    /// </summary>
    [Fact]
    public async Task Create_WhenCustomerAlreadyHasAccount_Throws()
    {
        var customers = new InMemoryCustomerRepository();
        var customer = await SaveCustomer(customers);
        var accounts = new InMemoryAccountRepository();
        var existing = Account.Open(customer.Id, "ACC-20261111-0001", 10m, Now);
        accounts.Accounts.Add(existing.AccountNumber, existing);
        var digits = new QueueDigits(2);
        var service = Service(customers, accounts, new FixedClock(Now), digits);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.Create(new CreateAccountRequest(customer.Id, 0m)));

        Assert.Equal("El cliente ya tiene una cuenta.", exception.Message);
        Assert.Equal(0, digits.Calls);
        Assert.Single(accounts.Accounts);
    }

    /// <summary>
    /// La cuenta de un cliente se encuentra por su id y devuelve número, saldo y fecha de apertura.
    /// </summary>
    [Fact]
    public async Task GetByCustomer_ReturnsTheAccount()
    {
        var customers = new InMemoryCustomerRepository();
        var customer = await SaveCustomer(customers);
        var accounts = new InMemoryAccountRepository();
        var service = Service(customers, accounts, new FixedClock(Now), new QueueDigits(7));
        var created = await service.Create(new CreateAccountRequest(customer.Id, 40m));

        var info = Assert.Single(await service.GetByCustomer(customer.Id));

        Assert.Equal(customer.Id, info.CustomerId);
        Assert.Equal(created.AccountNumber, info.AccountNumber);
        Assert.Equal(40m, info.Balance);
        Assert.Equal(Now, info.CreatedAt);
    }

    /// <summary>
    /// Si el cliente tiene varias cuentas antiguas, se devuelven todas por fecha de apertura.
    /// </summary>
    [Fact]
    public async Task GetByCustomer_WhenSeveralAccountsExist_ReturnsAllOrdered()
    {
        var customers = new InMemoryCustomerRepository();
        var customer = await SaveCustomer(customers);
        var accounts = new InMemoryAccountRepository();
        var later = Account.Open(customer.Id, "ACC-20261111-0002", 20m, Now);
        var earlier = Account.Open(customer.Id, "ACC-20261111-0001", 10m, Now.AddMinutes(-5));
        accounts.Accounts.Add(later.AccountNumber, later);
        accounts.Accounts.Add(earlier.AccountNumber, earlier);
        var service = Service(customers, accounts, new FixedClock(Now), new QueueDigits(1));

        var listed = await service.GetByCustomer(customer.Id);

        Assert.Equal(["ACC-20261111-0001", "ACC-20261111-0002"], listed.Select(account => account.AccountNumber));
    }

    /// <summary>
    /// Un cliente registrado sin cuenta no tiene información que devolver.
    /// </summary>
    [Fact]
    public async Task GetByCustomer_WhenCustomerHasNoAccount_Throws()
    {
        var customers = new InMemoryCustomerRepository();
        var customer = await SaveCustomer(customers);
        var service = Service(customers, new InMemoryAccountRepository(), new FixedClock(Now), new QueueDigits(1));

        var exception = await Assert.ThrowsAsync<CustomerAccountNotFoundException>(() => service.GetByCustomer(customer.Id));

        Assert.Equal(customer.Id, exception.CustomerId);
    }

    /// <summary>
    /// Buscar la cuenta de un cliente que no existe no consulta las cuentas.
    /// </summary>
    [Fact]
    public async Task GetByCustomer_WhenCustomerDoesNotExist_Throws()
    {
        var customerId = Guid.NewGuid();
        var accounts = new InMemoryAccountRepository();
        var service = Service(new InMemoryCustomerRepository(), accounts, new FixedClock(Now), new QueueDigits(1));

        var exception = await Assert.ThrowsAsync<CustomerNotFoundException>(() => service.GetByCustomer(customerId));

        Assert.Equal(customerId, exception.CustomerId);
        Assert.Equal(0, accounts.ListByCustomerCalls);
    }

    /// <summary>
    /// Depositar en una cuenta que no existe lanza AccountNotFoundException.
    /// </summary>
    [Fact]
    public async Task Deposit_WhenAccountDoesNotExist_Throws()
    {
        var accounts = new InMemoryAccountRepository();
        var service = Service(new InMemoryCustomerRepository(), accounts, new FixedClock(Now), new QueueDigits(1));

        var exception = await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            service.Deposit("ACC-20261111-0001", new AmountRequest(10m)));

        Assert.Equal("ACC-20261111-0001", exception.AccountNumber);
        Assert.Equal(0, accounts.UpdateCalls);
    }

    /// <summary>
    /// El historial sale por fecha ascendente. Si la fecha coincide, gana el orden de inserción.
    /// </summary>
    [Fact]
    public async Task GetTransactions_OrdersByTimeThenInsertion()
    {
        var customers = new InMemoryCustomerRepository();
        var customer = await SaveCustomer(customers);
        var accounts = new InMemoryAccountRepository();
        var clock = new FixedClock(Now);
        var service = Service(customers, accounts, clock, new QueueDigits(1));
        var created = await service.Create(new CreateAccountRequest(customer.Id, 10m));

        clock.UtcNow = Now;
        await service.Deposit(created.AccountNumber, new AmountRequest(5m));
        clock.UtcNow = Now.AddHours(1);
        await service.Deposit(created.AccountNumber, new AmountRequest(1m));

        var history = await service.GetTransactions(created.AccountNumber);

        Assert.Equal([10m, 5m, 1m], history.Select(line => line.Amount).ToArray());
        Assert.Equal(TransactionOption.Deposit, history[0].Type);
        Assert.Equal(Now, history[0].OccurredAt);
        Assert.Equal(Now, history[1].OccurredAt);
        Assert.Equal(Now.AddHours(1), history[2].OccurredAt);
    }

    private static AccountService Service(
        ICustomerRepository customers,
        IAccountRepository accounts,
        FixedClock clock,
        IAccountNumberDigits digits) =>
        new(customers, accounts, clock, digits);

    /// <summary>
    /// Una transferencia resta en el origen, suma en el destino y deja la cuenta contraparte en cada movimiento.
    /// </summary>
    [Fact]
    public async Task Transfer_MovesFundsBetweenAccounts()
    {
        var accounts = new InMemoryAccountRepository();
        var source = Account.Open(Guid.NewGuid(), "ACC-20261111-0001", 100m, Now);
        var destination = Account.Open(Guid.NewGuid(), "ACC-20261111-0002", 10m, Now);
        accounts.Accounts.Add(source.AccountNumber, source);
        accounts.Accounts.Add(destination.AccountNumber, destination);
        var service = Service(new InMemoryCustomerRepository(), accounts, new FixedClock(Now), new QueueDigits(1));

        var result = await service.Transfer(new TransferRequest(source.AccountNumber, destination.AccountNumber, 40m));

        Assert.Equal(60m, source.Balance);
        Assert.Equal(50m, destination.Balance);
        Assert.Equal(TransactionOption.TransferSent, result.Sent.Type);
        Assert.Equal(destination.AccountNumber, result.Sent.CounterpartyAccountNumber);
        Assert.Equal(60m, result.Sent.ResultingBalance);
        Assert.Equal(TransactionOption.TransferReceived, result.Received.Type);
        Assert.Equal(source.AccountNumber, result.Received.CounterpartyAccountNumber);
        Assert.Equal(50m, result.Received.ResultingBalance);
        Assert.Equal(1, accounts.UpdateCalls);
    }

    /// <summary>
    /// Sin fondos no cambia ninguna de las dos cuentas.
    /// </summary>
    [Fact]
    public async Task Transfer_WithoutFunds_LeavesBothBalancesUnchanged()
    {
        var accounts = new InMemoryAccountRepository();
        var source = Account.Open(Guid.NewGuid(), "ACC-20261111-0001", 30m, Now);
        var destination = Account.Open(Guid.NewGuid(), "ACC-20261111-0002", 10m, Now);
        accounts.Accounts.Add(source.AccountNumber, source);
        accounts.Accounts.Add(destination.AccountNumber, destination);
        var service = Service(new InMemoryCustomerRepository(), accounts, new FixedClock(Now), new QueueDigits(1));

        await Assert.ThrowsAsync<InsufficientFundsException>(() =>
            service.Transfer(new TransferRequest(source.AccountNumber, destination.AccountNumber, 40m)));

        Assert.Equal(30m, source.Balance);
        Assert.Equal(10m, destination.Balance);
        Assert.Equal(0, accounts.UpdateCalls);
    }

    /// <summary>
    /// No se transfiere de una cuenta hacia ella misma.
    /// </summary>
    [Fact]
    public async Task Transfer_WhenAccountsAreTheSame_Throws()
    {
        var accounts = new InMemoryAccountRepository();
        var source = Account.Open(Guid.NewGuid(), "ACC-20261111-0001", 100m, Now);
        accounts.Accounts.Add(source.AccountNumber, source);
        var service = Service(new InMemoryCustomerRepository(), accounts, new FixedClock(Now), new QueueDigits(1));

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.Transfer(new TransferRequest(source.AccountNumber, source.AccountNumber, 10m)));

        Assert.Equal("La cuenta origen y la destino deben ser distintas.", exception.Message);
        Assert.Equal(100m, source.Balance);
        Assert.Equal(0, accounts.UpdateCalls);
    }

    /// <summary>
    /// El listado de transferencias omite depósitos y retiros, conserva el orden e incluye el nombre del otro cliente.
    /// </summary>
    [Fact]
    public async Task GetTransfers_ReturnsOnlyTransfersInChronologicalOrder()
    {
        var customers = new InMemoryCustomerRepository();
        var ana = new Customer("Ana Pérez", new DateOnly(1990, 5, 2), Domain.Enums.Gender.Female, 1500m, Now);
        var luis = new Customer("Luis Gómez", new DateOnly(1985, 1, 9), Domain.Enums.Gender.Male, 2200m, Now);
        await customers.AddAsync(ana);
        await customers.AddAsync(luis);
        var accounts = new InMemoryAccountRepository();
        var source = Account.Open(ana.Id, "ACC-20261111-0001", 100m, Now);
        var destination = Account.Open(luis.Id, "ACC-20261111-0002", 0m, Now);
        accounts.Accounts.Add(source.AccountNumber, source);
        accounts.Accounts.Add(destination.AccountNumber, destination);
        var clock = new FixedClock(Now);
        var service = Service(customers, accounts, clock, new QueueDigits(1));
        await service.Transfer(new TransferRequest(source.AccountNumber, destination.AccountNumber, 25m));
        clock.UtcNow = Now.AddHours(1);
        await service.Deposit(source.AccountNumber, new AmountRequest(5m));
        clock.UtcNow = Now.AddHours(2);
        await service.Transfer(new TransferRequest(source.AccountNumber, destination.AccountNumber, 10m));

        var listed = await service.GetTransfers(source.AccountNumber);

        Assert.Equal([25m, 10m], listed.Select(movement => movement.Amount));
        Assert.All(listed, movement => Assert.Equal(TransactionOption.TransferSent, movement.Type));
        Assert.All(listed, movement => Assert.Equal("Luis Gómez", movement.CounterpartyCustomerName));
        Assert.Equal(Now, listed[0].OccurredAt);
        Assert.Equal(Now.AddHours(2), listed[1].OccurredAt);
    }

    private static async Task<Customer> SaveCustomer(InMemoryCustomerRepository customers)
    {
        var customer = new Customer("Ana Pérez", new DateOnly(1990, 5, 2), Domain.Enums.Gender.Female, 1500m, Now);
        await customers.AddAsync(customer);
        return customer;
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }

    private sealed class QueueDigits(params int[] values) : IAccountNumberDigits
    {
        private readonly Queue<int> _values = new(values);
        public int Calls { get; private set; }

        public int Next()
        {
            Calls++;
            return _values.Dequeue();
        }
    }

    private sealed class FixedDigits(int value) : IAccountNumberDigits
    {
        public int Calls { get; private set; }

        public int Next()
        {
            Calls++;
            return value;
        }
    }

    private sealed class InMemoryCustomerRepository : ICustomerRepository
    {
        private readonly List<Customer> _customers = [];

        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            _customers.Add(customer);
            return Task.CompletedTask;
        }

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_customers.SingleOrDefault(customer => customer.Id == id));

        public Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Customer>>(_customers);

        public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryAccountRepository : IAccountRepository
    {
        public Dictionary<string, Account> Accounts { get; } = new(StringComparer.Ordinal);
        public int UpdateCalls { get; private set; }
        public int ListByCustomerCalls { get; private set; }

        public Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            Accounts.Add(account.AccountNumber, account);
            return Task.CompletedTask;
        }

        public Task<Account?> GetByNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            Accounts.TryGetValue(accountNumber, out var account);
            return Task.FromResult(account);
        }

        public Task<IReadOnlyList<Account>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            ListByCustomerCalls++;
            return Task.FromResult<IReadOnlyList<Account>>(
                Accounts.Values.Where(account => account.CustomerId == customerId).ToArray());
        }

        public Task<bool> ExistsByNumberAsync(string accountNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(Accounts.ContainsKey(accountNumber));

        public Task<bool> ExistsByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Accounts.Values.Any(account => account.CustomerId == customerId));

        public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            Accounts[account.AccountNumber] = account;
            return Task.CompletedTask;
        }
    }

    private sealed class AlwaysTakenAccountRepository : IAccountRepository
    {
        public int ExistsCalls { get; private set; }
        public int AddCalls { get; private set; }

        public Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            AddCalls++;
            return Task.CompletedTask;
        }

        public Task<Account?> GetByNumberAsync(string accountNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<Account?>(null);

        public Task<IReadOnlyList<Account>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Account>>([]);

        public Task<bool> ExistsByNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            ExistsCalls++;
            return Task.FromResult(true);
        }

        public Task<bool> ExistsByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task UpdateAsync(Account account, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
