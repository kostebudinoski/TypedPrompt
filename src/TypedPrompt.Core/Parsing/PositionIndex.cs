using Tomlyn.Syntax;

namespace TypedPrompt.Core;

/// <summary>
/// Maps every key path in a TOML document (e.g. <c>user</c>, <c>variables.ticket</c>, <c>variables.ticket.default</c>)
/// to where it is written, by walking the syntax tree. Values are read from the model; positions come from here.
/// </summary>
internal sealed class PositionIndex
{
    private readonly Dictionary<string, SourcePosition> _positions = new(StringComparer.Ordinal);

    private PositionIndex()
    {
    }

    public static PositionIndex Build(DocumentSyntax document)
    {
        var index = new PositionIndex();
        index.Add(document.KeyValues, prefix: string.Empty);
        var arrayCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var table in document.Tables)
        {
            if (table.Name is null)
            {
                continue;
            }

            var path = Path(table.Name);
            var start = table.Name.Span.Start;
            var position = new SourcePosition(start.Line + 1, start.Column + 1, start.Line + 1, start.Column + 1, null);
            index.Record(path, position);

            // Each [[name]] table is one array element: index it as name[0], name[1], ... so a problem inside the
            // second element points at the second element, not the first.
            if (table is TableArraySyntax)
            {
                arrayCounts.TryGetValue(path, out var count);
                arrayCounts[path] = count + 1;
                path = $"{path}[{count}]";
                index.Record(path, position);
            }

            index.Add(table.Items, path + ".");
        }

        return index;
    }

    /// <summary>The position of <paramref name="path"/>, or of the closest enclosing path, or <see langword="null"/>.</summary>
    public SourcePosition? Find(string path)
    {
        for (var current = path; current.Length > 0; current = Parent(current))
        {
            if (_positions.TryGetValue(current, out var position))
            {
                return position;
            }
        }

        return null;
    }

    /// <summary>The first position recorded in the file, used when nothing more specific is known.</summary>
    public SourcePosition? First => _positions.Values.OrderBy(p => p.Line).ThenBy(p => p.Column).FirstOrDefault();

    private void Add(SyntaxList<KeyValueSyntax> items, string prefix)
    {
        foreach (var item in items)
        {
            if (item.Key is null)
            {
                continue;
            }

            var path = prefix + Path(item.Key);
            var key = item.Key.Span.Start;
            var value = item.Value?.Span.Start ?? key;
            var raw = item.Value is StringValueSyntax text ? text.Token?.Text : null;
            Record(path, new SourcePosition(key.Line + 1, key.Column + 1, value.Line + 1, value.Column + 1, raw));

            if (item.Value is InlineTableSyntax inline)
            {
                foreach (var entry in inline.Items)
                {
                    if (entry.KeyValue is { } keyValue)
                    {
                        Record(path + "." + (keyValue.Key is null ? string.Empty : Path(keyValue.Key)), Position(keyValue));
                    }
                }
            }
        }
    }

    private static SourcePosition Position(KeyValueSyntax item)
    {
        var key = item.Key!.Span.Start;
        var value = item.Value?.Span.Start ?? key;
        return new SourcePosition(key.Line + 1, key.Column + 1, value.Line + 1, value.Column + 1, (item.Value as StringValueSyntax)?.Token?.Text);
    }

    private void Record(string path, SourcePosition position)
    {
        if (!_positions.ContainsKey(path))
        {
            _positions[path] = position;
        }
    }

    private static string Path(KeySyntax key)
    {
        var parts = new List<string> { Name(key.Key) };
        foreach (var dotted in key.DotKeys)
        {
            parts.Add(Name(dotted.Key));
        }

        return string.Join(".", parts);
    }

    private static string Name(BareKeyOrStringValueSyntax? key) => key switch
    {
        BareKeySyntax bare => bare.Key?.Text ?? string.Empty,
        StringValueSyntax quoted => quoted.Value ?? string.Empty,
        _ => string.Empty,
    };

    /// <summary><c>examples[1].user</c> → <c>examples[1]</c> → <c>examples</c> → nothing.</summary>
    private static string Parent(string path)
    {
        var cut = Math.Max(path.LastIndexOf('.'), path.LastIndexOf('['));
        return cut < 0 ? string.Empty : path.Substring(0, cut);
    }
}
