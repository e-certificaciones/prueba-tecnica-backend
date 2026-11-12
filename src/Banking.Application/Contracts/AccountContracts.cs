using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Banking.Application.Contracts;

public sealed record CreateAccountRequest(
    [property: JsonPropertyName("idCliente")]
    [property: Description("Identificador del cliente")] Guid CustomerId,
    [property: JsonPropertyName("saldoInicial")]
    [property: Description("Saldo inicial")] decimal InitialBalance);

public sealed record AccountInfoResponse(
    [property: JsonPropertyName("idCliente")]
    [property: Description("Identificador del cliente")] Guid CustomerId,
    [property: JsonPropertyName("numeroCuenta")]
    [property: Description("Número de cuenta")] string AccountNumber,
    [property: JsonPropertyName("saldo")]
    [property: Description("Saldo")] decimal Balance,
    [property: JsonPropertyName("fechaCreacion")]
    [property: Description("Fecha de apertura, en UTC")] DateTime CreatedAt);

public sealed record AccountResponse(
    [property: JsonPropertyName("numeroCuenta")]
    [property: Description("Número de cuenta")] string AccountNumber,
    [property: JsonPropertyName("saldo")]
    [property: Description("Saldo")] decimal Balance);

public sealed record BalanceResponse(
    [property: JsonPropertyName("numeroCuenta")]
    [property: Description("Número de cuenta")] string AccountNumber,
    [property: JsonPropertyName("saldo")]
    [property: Description("Saldo actual")] decimal Balance);

public sealed record TransferRequest(
    [property: JsonPropertyName("cuentaOrigen")]
    [property: Description("Número de la cuenta que envía")] string SourceAccountNumber,
    [property: JsonPropertyName("cuentaDestino")]
    [property: Description("Número de la cuenta que recibe")] string DestinationAccountNumber,
    [property: JsonPropertyName("monto")]
    [property: Description("Monto a transferir")] decimal Amount);

public sealed record TransferMovementResponse(
    [property: JsonPropertyName("id")]
    [property: Description("Identificador del movimiento")] Guid Id,
    [property: JsonPropertyName("numeroCuenta")]
    [property: Description("Cuenta de este movimiento")] string AccountNumber,
    [property: JsonPropertyName("cuentaContraparte")]
    [property: Description("La otra cuenta de la transferencia")] string CounterpartyAccountNumber,
    [property: JsonPropertyName("nombreClienteContraparte")]
    [property: Description("Nombre del cliente de la otra cuenta")] string CounterpartyCustomerName,
    [property: JsonPropertyName("tipo")]
    [property: Description("Transferencia enviada o Transferencia recibida")] TransactionOption Type,
    [property: JsonPropertyName("monto")]
    [property: Description("Monto")] decimal Amount,
    [property: JsonPropertyName("fecha")]
    [property: Description("Fecha y hora de la transferencia, en UTC")] DateTime OccurredAt,
    [property: JsonPropertyName("saldoPosterior")]
    [property: Description("Saldo de esta cuenta después de la transferencia")] decimal ResultingBalance);

public sealed record TransferResultResponse(
    [property: JsonPropertyName("enviada")]
    [property: Description("Movimiento en la cuenta origen")] TransferMovementResponse Sent,
    [property: JsonPropertyName("recibida")]
    [property: Description("Movimiento en la cuenta destino")] TransferMovementResponse Received);

public sealed record AmountRequest(
    [property: JsonPropertyName("monto")]
    [property: Description("Monto del movimiento")] decimal Amount);

public sealed record TransactionResponse(
    [property: JsonPropertyName("id")]
    [property: Description("Identificador del movimiento")] Guid Id,
    [property: JsonPropertyName("tipo")]
    [property: Description("Tipo de movimiento: Depósito, Retiro, Transferencia enviada o Transferencia recibida")] TransactionOption Type,
    [property: JsonPropertyName("monto")]
    [property: Description("Monto")] decimal Amount,
    [property: JsonPropertyName("fecha")]
    [property: Description("Fecha y hora del movimiento, en UTC")] DateTime OccurredAt,
    [property: JsonPropertyName("saldoPosterior")]
    [property: Description("Saldo posterior al movimiento")] decimal ResultingBalance);
