using System.Text;

namespace TypedPrompt.Core;

/// <summary>Name rules shared by every target language.</summary>
public static class Identifiers
{
    /// <summary>Whether <paramref name="name"/> is a valid variable name: a letter or <c>_</c>, then letters, digits or <c>_</c> (ASCII).</summary>
    public static bool IsValidVariableName(string? name)
    {
        if (string.IsNullOrEmpty(name) || !(IsAsciiLetter(name![0]) || name[0] == '_'))
        {
            return false;
        }

        return name.All(c => IsAsciiLetter(c) || (c >= '0' && c <= '9') || c == '_');
    }

    /// <summary>
    /// Turns a file name into PascalCase words: <c>ticket-summarize</c> → <c>TicketSummarize</c>, <c>support_reply.v2</c>
    /// → <c>SupportReplyV2</c>. Separators are anything but ASCII letters and digits; the rest of each word is kept as
    /// written. Returns an empty string when nothing usable is left or the result would start with a digit.
    /// </summary>
    public static string ToPascalCase(string text)
    {
        var result = new StringBuilder();
        var startOfWord = true;
        foreach (var c in text ?? string.Empty)
        {
            if (!IsAsciiLetter(c) && !(c >= '0' && c <= '9'))
            {
                startOfWord = true;
                continue;
            }

            result.Append(startOfWord ? char.ToUpperInvariant(c) : c);
            startOfWord = false;
        }

        return result.Length > 0 && !(result[0] >= '0' && result[0] <= '9') ? result.ToString() : string.Empty;
    }

    /// <summary>The prompt name in a file path: the file name without <c>.prompt.toml</c> (or without its extension).</summary>
    public static string FileStem(string path)
    {
        path ??= string.Empty;
        var name = path.Substring(path.LastIndexOfAny(['/', '\\']) + 1);
        const string Suffix = ".prompt.toml";
        return name.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase)
            ? name.Substring(0, name.Length - Suffix.Length)
            : System.IO.Path.GetFileNameWithoutExtension(name);
    }

    private static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
}
