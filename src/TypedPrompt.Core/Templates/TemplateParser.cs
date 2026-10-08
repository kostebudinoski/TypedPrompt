using System.Text;

namespace TypedPrompt.Core;

/// <summary>
/// Splits a template into literal text and placeholders. The syntax is deliberately tiny: <c>{{name}}</c> (spaces inside
/// the braces allowed) inserts a variable, <c>\{{</c> writes a literal <c>{{</c>, and nothing else is special. No loops,
/// no conditions, no filters.
/// </summary>
public static class TemplateParser
{
    /// <summary>Parses <paramref name="text"/>. Errors do not stop parsing, so every error is found at once.</summary>
    public static (IReadOnlyList<TemplateSegment> Segments, IReadOnlyList<TemplateError> Errors) Parse(string text)
    {
        var segments = new List<TemplateSegment>();
        var errors = new List<TemplateError>();
        var literal = new StringBuilder();
        var literalStart = 0;

        void Flush(int next)
        {
            if (literal.Length > 0)
            {
                segments.Add(TemplateSegment.Literal(literal.ToString(), literalStart));
                literal.Clear();
            }

            literalStart = next;
        }

        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '\\' && string.CompareOrdinal(text, i + 1, "{{", 0, 2) == 0)
            {
                literal.Append("{{");
                i += 3;
                continue;
            }

            if (string.CompareOrdinal(text, i, "{{", 0, 2) != 0)
            {
                literal.Append(text[i]);
                i++;
                continue;
            }

            var end = text.IndexOf("}}", i + 2, StringComparison.Ordinal);
            if (end < 0)
            {
                errors.Add(new TemplateError(i, "'{{' is never closed with '}}'. Write \\{{ for a literal '{{'."));
                literal.Append(text, i, text.Length - i);
                break;
            }

            var name = text.Substring(i + 2, end - i - 2).Trim();
            if (Identifiers.IsValidVariableName(name))
            {
                Flush(i);
                segments.Add(TemplateSegment.Placeholder(name, i));
                literalStart = end + 2;
            }
            else
            {
                errors.Add(new TemplateError(i, $"'{{{{{name}}}}}' is not a valid placeholder: use a variable name (letters, digits, _). Write \\{{{{ for a literal '{{{{'."));
            }

            i = end + 2;
        }

        Flush(text.Length);
        return (segments, errors);
    }
}
