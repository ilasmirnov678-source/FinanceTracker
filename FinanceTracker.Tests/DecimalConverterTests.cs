using System.Globalization;
using System.Windows.Data;
using FinanceTracker.Converters;
using FluentAssertions;
using Xunit;

namespace FinanceTracker.Tests;

public class DecimalConverterTests
{
    private readonly DecimalConverter _converter = new();
    private static object Param => null!;

    [Fact]
    public void Convert_Decimal_ReturnsInvariantString()
    {
        _converter.Convert(123.45m, typeof(string), Param, CultureInfo.InvariantCulture)
            .Should().Be("123.45");
        _converter.Convert(0m, typeof(string), Param, CultureInfo.InvariantCulture)
            .Should().Be("0");
        _converter.Convert(-10.5m, typeof(string), Param, CultureInfo.InvariantCulture)
            .Should().Be("-10.5");
    }

    [Fact]
    public void Convert_Null_ReturnsEmptyString()
    {
        _converter.Convert(null!, typeof(string), Param, CultureInfo.InvariantCulture)
            .Should().Be(string.Empty);
    }

    [Fact]
    public void Convert_NonDecimal_ReturnsEmptyString()
    {
        _converter.Convert(42, typeof(string), Param, CultureInfo.InvariantCulture)
            .Should().Be(string.Empty);
        _converter.Convert("123", typeof(string), Param, CultureInfo.InvariantCulture)
            .Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertBack_Null_ReturnsNull()
    {
        _converter.ConvertBack(null!, typeof(decimal?), Param, CultureInfo.InvariantCulture)
            .Should().BeNull();
    }

    [Fact]
    public void ConvertBack_EmptyOrWhitespace_ReturnsNull()
    {
        _converter.ConvertBack("", typeof(decimal?), Param, CultureInfo.InvariantCulture)
            .Should().BeNull();
        _converter.ConvertBack("   ", typeof(decimal?), Param, CultureInfo.InvariantCulture)
            .Should().BeNull();
    }

    [Fact]
    public void ConvertBack_EndsWithDot_ReturnsDoNothing()
    {
        _converter.ConvertBack("10.", typeof(decimal?), Param, CultureInfo.InvariantCulture)
            .Should().Be(Binding.DoNothing);
    }

    [Fact]
    public void ConvertBack_EndsWithComma_ReturnsDoNothing()
    {
        _converter.ConvertBack("10,", typeof(decimal?), Param, CultureInfo.InvariantCulture)
            .Should().Be(Binding.DoNothing);
    }

    [Fact]
    public void ConvertBack_ValidInteger_ReturnsDecimal()
    {
        _converter.ConvertBack("100", typeof(decimal?), Param, CultureInfo.InvariantCulture)
            .Should().Be(100m);
    }

    [Fact]
    public void ConvertBack_ValidDecimalWithDot_ReturnsDecimal()
    {
        _converter.ConvertBack("123.45", typeof(decimal?), Param, CultureInfo.InvariantCulture)
            .Should().Be(123.45m);
    }

    [Fact]
    public void ConvertBack_ValidDecimalWithComma_ReturnsDecimal()
    {
        _converter.ConvertBack("123,45", typeof(decimal?), Param, CultureInfo.InvariantCulture)
            .Should().Be(123.45m);
    }

    [Fact]
    public void ConvertBack_InvalidString_ReturnsNull()
    {
        _converter.ConvertBack("abc", typeof(decimal?), Param, CultureInfo.InvariantCulture)
            .Should().BeNull();
        _converter.ConvertBack("12.34.56", typeof(decimal?), Param, CultureInfo.InvariantCulture)
            .Should().BeNull();
    }
}
