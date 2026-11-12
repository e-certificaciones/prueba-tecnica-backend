using System.Text.Json.Serialization;

namespace Banking.Application.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionOption
{
    [JsonStringEnumMemberName("Depósito")]
    Deposit,

    [JsonStringEnumMemberName("Retiro")]
    Withdrawal,

    [JsonStringEnumMemberName("Transferencia enviada")]
    TransferSent,

    [JsonStringEnumMemberName("Transferencia recibida")]
    TransferReceived
}
