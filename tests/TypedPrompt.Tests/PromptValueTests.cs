using System.Globalization;

namespace TypedPrompt.Tests;

public class PromptValueTests
{
    [Theory]
    [InlineData(42.0, "42")]
    [InlineData(0.2, "0.2")]
    [InlineData(-1.5, "-1.5")]
    [InlineData(1234567.0, "1234567")]
    [InlineData(0.1 + 0.2, "0.30000000000000004")]
    public void Numbers_use_their_shortest_exact_form(double value, string expected) =>
        Assert.Equal(expected, PromptValue.Format(value));

    [Fact]
    public void Numbers_ignore_the_current_culture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("0.2", PromptValue.Format(0.2));
            Assert.Equal("1234567.5", PromptValue.Format(1234567.5));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Numbers_that_are_not_finite_are_refused(double value) =>
        Assert.Throws<ArgumentException>(() => PromptValue.Format(value));

    [Fact]
    public void Booleans_are_lowercase() =>
        Assert.Equal(("true", "false"), (PromptValue.Format(true), PromptValue.Format(false)));

    [Fact]
    public void Missing_values_are_empty_text()
    {
        Assert.Equal(string.Empty, PromptValue.Format((string?)null));
        Assert.Equal(string.Empty, PromptValue.Format((double?)null));
        Assert.Equal(string.Empty, PromptValue.Format((bool?)null));
    }

    [Fact]
    public void Text_is_kept_as_it_is() =>
        Assert.Equal("  Größe {{x}} \n", PromptValue.Format("  Größe {{x}} \n"));
}
