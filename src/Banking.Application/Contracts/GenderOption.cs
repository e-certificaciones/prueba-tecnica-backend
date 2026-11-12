using System.Text.Json.Serialization;

namespace Banking.Application.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GenderOption
{
    [JsonStringEnumMemberName("Masculino")]
    Male,

    [JsonStringEnumMemberName("Femenino")]
    Female,

    [JsonStringEnumMemberName("Otro")]
    Other
}
