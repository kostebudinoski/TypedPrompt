using System.Globalization;

namespace TypedPrompt;

/// <summary>
/// Turns variable values into text the same way on every machine: numbers with a dot and no thousands separator
/// (<c>0.2</c>, never <c>0,2</c>), booleans as <c>true</c>/<c>false</c>, a missing value as empty text.
/// Generated <c>Render</c> methods call this; you rarely need it directly.
/// </summary>
public static class PromptValue
{
    /// <summary>Text as it is; <see langword="null"/> as empty text.</summary>
    public static string Format(string? value) => value ?? string.Empty;

    /// <summary>A number in its shortest exact form: <c>42</c>, <c>0.2</c>, <c>1E+21</c>.</summary>
    /// <exception cref="ArgumentException">The number is NaN or infinite, which has no sensible text in a prompt.</exception>
    public static string Format(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException($"{value.ToString(CultureInfo.InvariantCulture)} cannot be used in a prompt.", nameof(value));
        }

        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>A number, or empty text when <see langword="null"/>.</summary>
    public static string Format(double? value) => value is { } number ? Format(number) : string.Empty;

    /// <summary><c>true</c> or <c>false</c>.</summary>
    public static string Format(bool value) => value ? "true" : "false";

    /// <summary><c>true</c>, <c>false</c>, or empty text when <see langword="null"/>.</summary>
    public static string Format(bool? value) => value is { } flag ? Format(flag) : string.Empty;
}
