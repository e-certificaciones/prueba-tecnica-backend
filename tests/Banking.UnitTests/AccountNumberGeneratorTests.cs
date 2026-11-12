using Banking.Domain;
using Banking.Domain.Exceptions;

namespace Banking.UnitTests;

/// <summary>
/// Integridad del generador, como pide el enunciado:
/// longitud correcta, prefijo correcto, fecha de hoy y unicidad básica.
/// </summary>
public class AccountNumberGeneratorTests
{
    private readonly AccountNumberGenerator _generator = new();

    /// <summary>
    /// Un número generado hoy cumple las cuatro comprobaciones.
    /// ACC-YYYYMMDD-XXXX tiene 17 caracteres. Dos sufijos distintos no coinciden.
    /// </summary>
    [Fact]
    public void Generate_HasCorrectLengthPrefixTodayAndBasicUniqueness()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayText = today.ToString("yyyyMMdd");

        var first = _generator.Generate(today, 1204);
        var second = _generator.Generate(today, 8731);

        // Longitud correcta: ACC- + YYYYMMDD + - + XXXX.
        Assert.Equal(17, first.Length);
        Assert.Equal(17, second.Length);

        // Prefijo correcto.
        Assert.StartsWith("ACC-", first);
        Assert.StartsWith("ACC-", second);

        // Fecha de hoy, en UTC, en el segmento YYYYMMDD.
        Assert.Equal(todayText, first.Substring(4, 8));
        Assert.Equal(todayText, second.Substring(4, 8));

        // Unicidad básica: el mismo día con otros 4 dígitos produce otro número.
        Assert.NotEqual(first, second);
    }

    /// <summary>
    /// 0 y 9999 son válidos. Siempre se escriben con 4 dígitos: 0000 y 9999.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(9999)]
    public void Generate_AcceptsDigitsAtRangeEdges(int fourDigits)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var number = _generator.Generate(today, fourDigits);

        Assert.EndsWith(fourDigits.ToString("0000"), number);
    }

    /// <summary>
    /// Un valor fuera de 0 a 9999 no tiene 4 dígitos y se rechaza.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(10000)]
    public void Generate_RejectsDigitsOutsideRange(int fourDigits)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<InvalidAmountException>(() => _generator.Generate(today, fourDigits));
    }
}
