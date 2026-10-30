using Kwarta.Services;

namespace Kwarta.Tests;

public class FormattingTests
{
    [Theory]
    [InlineData(1234.5, "₱1,234.50")]
    [InlineData(-15, "-₱15.00")]
    [InlineData(0, "₱0.00")]
    public void Money_Format(decimal amount, string expected) => Assert.Equal(expected, Money.Format(amount));

    [Theory]
    [InlineData("₱1,234.50", 1234.5)]
    [InlineData(" 15 ", 15)]
    [InlineData("-200", -200)]
    public void Money_TryParse_accepts_symbols_and_separators(string text, decimal expected)
    {
        Assert.True(Money.TryParse(text, out var amount));
        Assert.Equal(expected, amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData(null)]
    public void Money_TryParse_rejects_non_numbers(string? text) => Assert.False(Money.TryParse(text, out _));

    [Fact]
    public void Csv_quotes_fields_with_commas_quotes_or_line_breaks()
    {
        Assert.Equal("plain,\"a,b\",\"say \"\"hi\"\"\",\"two\nlines\"",
            Csv.Row("plain", "a,b", "say \"hi\"", "two\nlines"));
    }
}
