using Banking.Domain.Enums;
using Banking.Domain.Exceptions;

namespace Banking.Application.Contracts;

internal static class ContractMapping
{
    public static Gender ToDomain(GenderOption gender) => gender switch
    {
        GenderOption.Male => Gender.Male,
        GenderOption.Female => Gender.Female,
        GenderOption.Other => Gender.Other,
        _ => throw new BusinessException("El género no es válido.")
    };

    public static GenderOption ToOption(Gender gender) => gender switch
    {
        Gender.Male => GenderOption.Male,
        Gender.Female => GenderOption.Female,
        Gender.Other => GenderOption.Other,
        _ => throw new BusinessException("El género no es válido.")
    };

    public static TransactionOption ToOption(TransactionType type) => type switch
    {
        TransactionType.Deposit => TransactionOption.Deposit,
        TransactionType.Withdrawal => TransactionOption.Withdrawal,
        TransactionType.TransferSent => TransactionOption.TransferSent,
        TransactionType.TransferReceived => TransactionOption.TransferReceived,
        _ => throw new BusinessException("El tipo de movimiento no es válido.")
    };
}
