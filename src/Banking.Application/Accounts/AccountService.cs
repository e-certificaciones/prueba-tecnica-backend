using Banking.Application.Contracts;
using Banking.Domain;
using Banking.Domain.Abstractions;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;

namespace Banking.Application.Accounts;

public sealed class AccountService(
    ICustomerRepository customers,
    IAccountRepository accounts,
    IClock clock,
    IAccountNumberDigits digits) : IAccountService
{
    private const int MaxAttempts = 10;
    private readonly AccountNumberGenerator _generator = new();

    public async Task<AccountResponse> Create(CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            throw new CustomerNotFoundException(request.CustomerId);

        if (await accounts.ExistsByCustomerAsync(request.CustomerId, cancellationToken))
            throw new BusinessException("El cliente ya tiene una cuenta.");

        if (request.InitialBalance < 0)
            throw new InvalidAmountException("El saldo inicial no puede ser negativo.");

        var createdAt = clock.UtcNow;
        var createdOn = DateOnly.FromDateTime(createdAt);

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var accountNumber = _generator.Generate(createdOn, digits.Next());
            if (await accounts.ExistsByNumberAsync(accountNumber, cancellationToken))
                continue;

            var account = Account.Open(request.CustomerId, accountNumber, request.InitialBalance, createdAt);
            await accounts.AddAsync(account, cancellationToken);
            return new AccountResponse(account.AccountNumber, account.Balance);
        }

        throw new AccountNumberUnavailableException();
    }

    public async Task<IReadOnlyList<AccountInfoResponse>> GetByCustomer(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
            throw new CustomerNotFoundException(customerId);

        var found = await accounts.ListByCustomerAsync(customerId, cancellationToken);
        if (found.Count == 0)
            throw new CustomerAccountNotFoundException(customerId);

        return found
            .OrderBy(account => account.CreatedAt)
            .ThenBy(account => account.AccountNumber, StringComparer.Ordinal)
            .Select(account => new AccountInfoResponse(account.CustomerId, account.AccountNumber, account.Balance, account.CreatedAt))
            .ToArray();
    }

    public async Task<BalanceResponse> GetBalance(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAccount(accountNumber, cancellationToken);
        return new BalanceResponse(account.AccountNumber, account.Balance);
    }

    public async Task<TransactionResponse> Deposit(string accountNumber, AmountRequest request, CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAccount(accountNumber, cancellationToken);
        var transaction = account.Deposit(request.Amount, clock.UtcNow);
        await accounts.UpdateAsync(account, cancellationToken);
        return ToResponse(transaction);
    }

    public async Task<TransactionResponse> Withdraw(string accountNumber, AmountRequest request, CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAccount(accountNumber, cancellationToken);
        var transaction = account.Withdraw(request.Amount, clock.UtcNow);
        await accounts.UpdateAsync(account, cancellationToken);
        return ToResponse(transaction);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetTransactions(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAccount(accountNumber, cancellationToken);

        return account.Transactions
            .Select((transaction, index) => (transaction, index))
            .OrderBy(item => item.transaction.OccurredAt)
            .ThenBy(item => item.index)
            .Select(item => ToResponse(item.transaction))
            .ToArray();
    }

    public async Task<TransferResultResponse> Transfer(TransferRequest request, CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.SourceAccountNumber, request.DestinationAccountNumber, StringComparison.Ordinal))
            throw new BusinessException("La cuenta origen y la destino deben ser distintas.");

        var source = await GetRequiredAccount(request.SourceAccountNumber, cancellationToken);
        var destination = await GetRequiredAccount(request.DestinationAccountNumber, cancellationToken);
        var occurredAt = clock.UtcNow;
        var sent = source.SendTransfer(request.Amount, occurredAt, destination.AccountNumber);
        var received = destination.ReceiveTransfer(request.Amount, occurredAt, source.AccountNumber);
        await accounts.UpdateAsync(source, cancellationToken);

        var sourceCustomer = await customers.GetByIdAsync(source.CustomerId, cancellationToken);
        var destinationCustomer = await customers.GetByIdAsync(destination.CustomerId, cancellationToken);

        return new TransferResultResponse(
            ToTransfer(source.AccountNumber, sent, destinationCustomer?.FullName ?? string.Empty),
            ToTransfer(destination.AccountNumber, received, sourceCustomer?.FullName ?? string.Empty));
    }

    public async Task<IReadOnlyList<TransferMovementResponse>> GetTransfers(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAccount(accountNumber, cancellationToken);

        var movements = account.Transactions
            .Select((transaction, index) => (transaction, index))
            .Where(item => item.transaction.Type is TransactionType.TransferSent or TransactionType.TransferReceived)
            .OrderBy(item => item.transaction.OccurredAt)
            .ThenBy(item => item.index)
            .ToArray();

        var names = await NamesByAccountAsync(
            movements.Select(item => item.transaction.CounterpartyAccountNumber),
            cancellationToken);

        return movements
            .Select(item => ToTransfer(
                account.AccountNumber,
                item.transaction,
                NameOf(names, item.transaction.CounterpartyAccountNumber)))
            .ToArray();
    }

    private async Task<Account> GetRequiredAccount(string accountNumber, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByNumberAsync(accountNumber, cancellationToken);
        if (account is null)
            throw new AccountNotFoundException(accountNumber);

        return account;
    }

    private static TransactionResponse ToResponse(Transaction transaction) =>
        new(
            transaction.Id,
            ContractMapping.ToOption(transaction.Type),
            transaction.Amount,
            transaction.OccurredAt,
            transaction.ResultingBalance);

    private async Task<IReadOnlyDictionary<string, string>> NamesByAccountAsync(
        IEnumerable<string?> accountNumbers,
        CancellationToken cancellationToken)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var number in accountNumbers.Where(number => !string.IsNullOrWhiteSpace(number)).Distinct(StringComparer.Ordinal))
        {
            var account = await accounts.GetByNumberAsync(number!, cancellationToken);
            if (account is null)
                continue;

            var customer = await customers.GetByIdAsync(account.CustomerId, cancellationToken);
            if (customer is not null)
                names[account.AccountNumber] = customer.FullName;
        }

        return names;
    }

    private static string NameOf(IReadOnlyDictionary<string, string> names, string? accountNumber) =>
        accountNumber is not null && names.TryGetValue(accountNumber, out var name) ? name : string.Empty;

    private static TransferMovementResponse ToTransfer(string accountNumber, Transaction transaction, string counterpartyCustomerName) =>
        new(
            transaction.Id,
            accountNumber,
            transaction.CounterpartyAccountNumber ?? string.Empty,
            counterpartyCustomerName,
            ContractMapping.ToOption(transaction.Type),
            transaction.Amount,
            transaction.OccurredAt,
            transaction.ResultingBalance);
}
