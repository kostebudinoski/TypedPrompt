using System.Globalization;
using System.Text;

namespace TypedPrompt.Core;

/// <summary>C# naming and literal rules used by <see cref="CSharpEmitter"/>.</summary>
internal static class CSharpSyntax
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while",
    };

    /// <summary><c>max_sentences</c> → <c>MaxSentences</c>.</summary>
    public static string PropertyName(string variable)
    {
        var name = Identifiers.ToPascalCase(variable);
        return name.Length == 0 ? "Value" : name;
    }

    /// <summary><c>max_sentences</c> → <c>maxSentences</c>; C# keywords get an <c>@</c>: <c>class</c> → <c>@class</c>.</summary>
    public static string ParameterName(string variable)
    {
        var property = PropertyName(variable);
        var name = char.ToLowerInvariant(property[0]) + property.Substring(1);
        return Keywords.Contains(name) ? "@" + name : name;
    }

    /// <summary>A C# string literal: <c>"..."</c> with escapes, or <c>null</c>.</summary>
    public static string Literal(string? value)
    {
        if (value is null)
        {
            return "null";
        }

        var result = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': result.Append("\\\""); break;
                case '\\': result.Append("\\\\"); break;
                case '\n': result.Append("\\n"); break;
                case '\r': result.Append("\\r"); break;
                case '\t': result.Append("\\t"); break;
                case '\0': result.Append("\\0"); break;
                default:
                    if (char.IsControl(c) || c == (char)0x2028 || c == (char)0x2029)
                    {
                        result.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        result.Append(c);
                    }

                    break;
            }
        }

        return result.Append('"').ToString();
    }

    /// <summary>A C# double literal in round-trip form: <c>3D</c>, <c>0.2D</c>.</summary>
    public static string Literal(double value) => value.ToString("R", CultureInfo.InvariantCulture) + "D";

    /// <summary>A C# bool literal.</summary>
    public static string Literal(bool value) => value ? "true" : "false";

    /// <summary>A value of a variable's kind as a C# literal.</summary>
    public static string Literal(object? value) => value switch
    {
        null => "null",
        string text => Literal(text),
        double number => Literal(number),
        bool flag => Literal(flag),
        _ => throw new ArgumentException($"Unexpected value type {value.GetType().Name}.", nameof(value)),
    };

    /// <summary>Text safe inside an XML doc comment.</summary>
    public static string Xml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r", string.Empty).Replace("\n", " ");
}
