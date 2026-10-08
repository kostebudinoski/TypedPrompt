using System.Globalization;

namespace TypedPrompt.Core;

/// <summary>F# naming and literal rules used by <see cref="FSharpEmitter"/>.</summary>
internal static class FSharpSyntax
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "abstract", "and", "as", "assert", "base", "begin", "class", "default", "delegate", "do", "done", "downcast",
        "downto", "elif", "else", "end", "exception", "extern", "false", "finally", "fixed", "for", "fun", "function",
        "global", "if", "in", "inherit", "inline", "interface", "internal", "lazy", "let", "match", "member", "module",
        "mutable", "namespace", "new", "not", "null", "of", "open", "or", "override", "private", "public", "rec", "return",
        "select", "sig", "static", "struct", "then", "to", "true", "try", "type", "upcast", "use", "val", "void", "when",
        "while", "with", "yield", "const", "break", "checked", "component", "constraint", "continue", "event", "external",
        "include", "mixin", "parallel", "process", "protected", "pure", "sealed", "tailcall", "trait", "virtual",
    };

    /// <summary><c>max_sentences</c> → <c>MaxSentences</c> (same as C#).</summary>
    public static string PropertyName(string variable) => CSharpSyntax.PropertyName(variable);

    /// <summary>The plain camelCase name, e.g. <c>maxSentences</c>; used in docs and messages.</summary>
    public static string PlainParameterName(string variable)
    {
        var property = PropertyName(variable);
        return char.ToLowerInvariant(property[0]) + property.Substring(1);
    }

    /// <summary>The parameter name as written in code: F# keywords get double backticks, e.g. <c>``type``</c>.</summary>
    public static string ParameterName(string variable)
    {
        var name = PlainParameterName(variable);
        return Keywords.Contains(name) ? "``" + name + "``" : name;
    }

    /// <summary>An F# string literal; F# uses the same escapes as C#.</summary>
    public static string Literal(string? value) => CSharpSyntax.Literal(value);

    /// <summary>An F# float literal: always with a decimal point or exponent, e.g. <c>3.0</c>, <c>0.2</c>, <c>1E+21</c>.</summary>
    public static string Literal(double value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        return text.IndexOfAny(['.', 'E', 'e']) >= 0 ? text : text + ".0";
    }

    /// <summary>An F# bool literal.</summary>
    public static string Literal(bool value) => value ? "true" : "false";

    /// <summary>A default value boxed as <c>obj</c>, for <c>VariableInfo</c>.</summary>
    public static string Boxed(object? value) => value switch
    {
        null => "null",
        string text => $"box {Literal(text)}",
        double number => $"box {Literal(number)}",
        bool flag => $"box {Literal(flag)}",
        _ => throw new ArgumentException($"Unexpected value type {value.GetType().Name}.", nameof(value)),
    };

    /// <summary>A default value as an F# literal of the variable's type.</summary>
    public static string Literal(object value) => value switch
    {
        string text => Literal(text),
        double number => Literal(number),
        bool flag => Literal(flag),
        _ => throw new ArgumentException($"Unexpected value type {value.GetType().Name}.", nameof(value)),
    };
}
