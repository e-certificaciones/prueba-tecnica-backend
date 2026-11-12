using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Banking.Application.Contracts;

public sealed record CreateCustomerRequest(
    [property: JsonPropertyName("nombreCompleto")]
    [property: Description("Nombre completo")] string FullName,
    [property: JsonPropertyName("fechaNacimiento")]
    [property: Description("Fecha de nacimiento")] DateOnly DateOfBirth,
    [property: JsonPropertyName("genero")]
    [property: Description("Sexo o género: Masculino, Femenino u Otro")] GenderOption Gender,
    [property: JsonPropertyName("ingresosMensuales")]
    [property: Description("Ingresos mensuales")] decimal MonthlyIncome);

public sealed record CustomerResponse(
    [property: JsonPropertyName("id")]
    [property: Description("Identificador del cliente")] Guid Id,
    [property: JsonPropertyName("nombreCompleto")]
    [property: Description("Nombre completo")] string FullName,
    [property: JsonPropertyName("fechaNacimiento")]
    [property: Description("Fecha de nacimiento")] DateOnly DateOfBirth,
    [property: JsonPropertyName("genero")]
    [property: Description("Sexo o género")] GenderOption Gender,
    [property: JsonPropertyName("ingresosMensuales")]
    [property: Description("Ingresos mensuales")] decimal MonthlyIncome,
    [property: JsonPropertyName("fechaCreacion")]
    [property: Description("Fecha de registro, en UTC")] DateTime CreatedAt);
