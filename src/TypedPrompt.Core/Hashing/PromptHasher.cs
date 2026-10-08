using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TypedPrompt.Core;

/// <summary>
/// Computes a prompt's content hash at build time: SHA-256 (lowercase hex) of a fixed text form of everything that
/// changes what the model receives or how it is called. Change one word and the hash changes; change nothing and it
/// stays the same, so the hash identifies the exact prompt behind an output.
/// </summary>
/// <remarks>
/// <para>Covered: system text, examples, user text, variables (name, type, default), model settings, output schema.
/// Not covered (they don't change behaviour): key, description, tags, metadata, the <c>version</c> label, variable
/// descriptions and <c>required</c>.</para>
/// <para>Texts are already normalised by the parser (<c>\n</c> line endings, trimmed). Numbers use <c>G17</c>, which is
/// identical on .NET Framework (Visual Studio's compiler) and modern .NET (<c>dotnet build</c>).</para>
/// </remarks>
public static class PromptHasher
{
    /// <summary>The hash: 64 lowercase hex characters.</summary>
    public static string Compute(ParsedPrompt prompt)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(CanonicalText(prompt)));
        var hex = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            hex.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return hex.ToString();
    }

    /// <summary>The exact text the hash is computed from: one field per line, every text value JSON-quoted.</summary>
    public static string CanonicalText(ParsedPrompt prompt)
    {
        if (prompt is null)
        {
            throw new ArgumentNullException(nameof(prompt));
        }

        var text = new StringBuilder();
        void Line(string name, string value) => text.Append(name).Append(' ').Append(value).Append('\n');

        Line("typedprompt", "1");
        Line("system", Quote(prompt.System?.Text));
        foreach (var example in prompt.Examples)
        {
            Line("example", Quote(example.User.Text) + " " + Quote(example.Assistant.Text));
        }

        Line("user", Quote(prompt.User.Text));
        foreach (var variable in prompt.Variables.OrderBy(v => v.Name, StringComparer.Ordinal))
        {
            Line("variable", $"{Quote(variable.Name)} {variable.Kind.ToString().ToLowerInvariant()} {Value(variable.Default)}");
        }

        if (prompt.Model is { } model)
        {
            Line("model.name", Quote(model.Name));
            Line("model.temperature", Value(model.Temperature));
            Line("model.top_p", Value(model.TopP));
            Line("model.top_k", Value((double?)model.TopK));
            Line("model.max_output_tokens", Value((double?)model.MaxOutputTokens));
            Line("model.stop_sequences", model.StopSequences is null ? "null" : string.Join(" ", model.StopSequences.Select(Quote)));
            Line("model.effort", Quote(model.Effort));
        }

        Line("output", Quote(prompt.OutputSchema));
        return text.ToString();
    }

    private static string Value(object? value) => value switch
    {
        null => "null",
        string text => Quote(text),
        double number => number.ToString("G17", CultureInfo.InvariantCulture),
        bool flag => flag ? "true" : "false",
        _ => Quote(Convert.ToString(value, CultureInfo.InvariantCulture)),
    };

    /// <summary>A JSON string literal (or <c>null</c>), so values can never run into each other.</summary>
    private static string Quote(string? value)
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
                default:
                    if (c < ' ')
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
}
