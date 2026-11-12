using Banking.Application.Contracts;

namespace Banking.Application.Accounts;

public interface IAccountService
{
    Task<AccountResponse> Create(CreateAccountRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountInfoResponse>> GetByCustomer(Guid customerId, CancellationToken cancellationToken = default);

    Task<BalanceResponse> GetBalance(string accountNumber, CancellationToken cancellationToken = default);

    Task<TransactionResponse> Deposit(string accountNumber, AmountRequest request, CancellationToken cancellationToken = default);

    Task<TransactionResponse> Withdraw(string accountNumber, AmountRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionResponse>> GetTransactions(string accountNumber, CancellationToken cancellationToken = default);

    Task<TransferResultResponse> Transfer(TransferRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransferMovementResponse>> GetTransfers(string accountNumber, CancellationToken cancellationToken = default);
}
