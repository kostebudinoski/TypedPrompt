using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace TypedPrompt.Core;

/// <summary>
/// Reads one <c>*.prompt.toml</c> file into a <see cref="ParsedPrompt"/>. Strict: unknown fields, wrong value types,
/// undeclared placeholders and malformed schemas are errors. Every problem is collected, with its line and column, so a
/// file with several mistakes reports all of them at once.
/// </summary>
public static class PromptFileParser
{
    private static readonly string[] TopLevelFields = ["key", "version", "description", "tags", "system", "examples", "user", "variables", "model", "output", "metadata"];
    private static readonly string[] ExampleFields = ["user", "assistant"];

    /// <summary>The most common TOML surprise: a top-level field written below a section header ends up inside that section.</summary>
    private const string SectionHint =
        "Hint: below a [section] or [[examples]] header, every key belongs to that section. Write the top-level fields (key, version, description, tags, system, user) above the first [section].";
    private static readonly string[] VariableFields = ["type", "required", "default", "description"];
    private static readonly string[] ModelFields = ["name", "temperature", "top_p", "top_k", "max_output_tokens", "stop_sequences", "effort"];
    private static readonly string[] EffortLevels = ["minimal", "low", "medium", "high", "max"];
    private static readonly string[] OutputFields = ["schema"];

    /// <summary>Parses and validates one file.</summary>
    /// <param name="path">The file path, used for the key, the class name and messages.</param>
    /// <param name="text">The file contents.</param>
    public static PromptParseResult Parse(string path, string text)
    {
        var problems = new List<PromptProblem>();
        var document = Toml.Parse(text ?? string.Empty, path);
        if (document.HasErrors)
        {
            return new PromptParseResult(null, SyntaxProblems(path, document.Diagnostics));
        }

        if (!Toml.TryToModel<TomlTable>(document, out var root, out var diagnostics))
        {
            return new PromptParseResult(null, SyntaxProblems(path, diagnostics));
        }

        var prompt = new Reader(path, PositionIndex.Build(document), problems).Read(root);
        var ordered = problems.OrderBy(p => p.Line).ThenBy(p => p.Column).ToList();
        return new PromptParseResult(ordered.Any(p => p.Severity == ProblemSeverity.Error) ? null : prompt, ordered);
    }

    /// <summary>One problem per position: Tomlyn often reports a single mistake twice (e.g. "invalid newline" and "end of file").</summary>
    private static List<PromptProblem> SyntaxProblems(string path, DiagnosticsBag diagnostics) =>
        diagnostics
            .Where(d => d.Kind == DiagnosticMessageKind.Error)
            .Select(d => SyntaxProblem(path, d))
            .GroupBy(p => (p.Line, p.Column))
            .Select(g => g.First())
            .ToList();

    private static PromptProblem SyntaxProblem(string path, DiagnosticMessage diagnostic)
    {
        var message = diagnostic.Message;
        if (message.IndexOf("escape", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            message += " Hint: inside \"...\" or \"\"\"...\"\"\" a backslash starts a TOML escape; write \\\\{{ there, or use '''...''' where \\{{ is kept as written.";
        }
        else if (message.IndexOf("already defined", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            message += " " + SectionHint;
        }

        var start = diagnostic.Span.Start;
        return new PromptProblem(PromptProblems.InvalidToml, path, start.Line + 1, start.Column + 1, message);
    }

    private sealed class Reader(string path, PositionIndex index, List<PromptProblem> problems)
    {
        private readonly Dictionary<string, (string Value, int Lead)> _texts = new(StringComparer.Ordinal);

        public ParsedPrompt? Read(TomlTable root)
        {
            CheckKeys(root, string.Empty, TopLevelFields, "at the top level");

            var stem = Identifiers.FileStem(path);
            var name = Identifiers.ToPascalCase(stem);
            if (name.Length == 0)
            {
                Add(PromptProblems.InvalidFileName, null,
                    $"'{System.IO.Path.GetFileName(path)}' cannot be turned into a class name. Use letters and digits, e.g. ticket-summarize.prompt.toml.");
            }

            var key = Text(root, "key", "key") ?? stem;
            if (key.Length == 0 || key.Any(char.IsWhiteSpace))
            {
                Add(PromptProblems.InvalidValue, "key", $"'{key}' is not a valid key: it must not be empty or contain spaces.");
            }

            var version = Text(root, "version", "version")?.Trim();
            if (version is not null && (version.Length == 0 || version.Any(char.IsWhiteSpace)))
            {
                Add(PromptProblems.InvalidValue, "version", $"'{version}' is not a valid version label: it must not be empty or contain spaces, e.g. version = \"2.1\".");
                version = null;
            }

            var description = Text(root, "description", "description");
            var tags = TextList(root, "tags", "tags") ?? [];
            var variables = Variables(root);
            var system = TemplateField(root, "system", required: false);
            var examples = Examples(root);
            var user = TemplateField(root, "user", required: true);
            var model = Model(root);
            var schema = OutputSchema(root);
            var metadata = Metadata(root);

            var templates = new List<(string, Template?)> { ("system", system) };
            foreach (var (example, examplePath) in examples)
            {
                templates.Add((examplePath + ".user", example.User));
                templates.Add((examplePath + ".assistant", example.Assistant));
            }

            templates.Add(("user", user));
            CheckPlaceholders(variables, templates);

            return user is null
                ? null
                : new ParsedPrompt(path, key, name, description?.Trim(), tags, metadata, system, user,
                    examples.Select(e => e.Example).ToList(), variables, model, schema, version);
        }

        private void CheckPlaceholders(IReadOnlyList<ParsedVariable> variables, IEnumerable<(string Field, Template? Template)> templates)
        {
            var declared = new HashSet<string>(variables.Select(v => v.Name), StringComparer.Ordinal);
            var used = new HashSet<string>(StringComparer.Ordinal);

            foreach (var (field, template) in templates)
            {
                foreach (var placeholder in template?.Segments.Where(s => s.Kind == TemplateSegmentKind.Placeholder) ?? [])
                {
                    used.Add(placeholder.Value);
                    if (!declared.Contains(placeholder.Value))
                    {
                        var hint = Suggestions.Closest(placeholder.Value, declared) is { } close
                            ? $"Did you mean '{close}'?"
                            : $"Declare it: [variables.{placeholder.Value}] with type = \"text\".";
                        AddInText(PromptProblems.UndeclaredPlaceholder, field, placeholder.Offset,
                            $"{{{{{placeholder.Value}}}}} is not declared in [variables]. {hint}");
                    }
                }
            }

            foreach (var variable in variables.Where(v => !used.Contains(v.Name)))
            {
                Add(PromptProblems.UnusedVariable, "variables." + variable.Name,
                    $"Variable '{variable.Name}' is declared but no template uses it. Remove it, or use {{{{{variable.Name}}}}} in system, user or an example.");
            }
        }

        private Template? TemplateField(TomlTable root, string field, bool required) =>
            required
                ? TemplateField(root, field, field, PromptProblems.MissingUser, null, "'user' is required: the request sent to the model, e.g. user = \"{{ticket}}\". " + SectionHint)
                : TemplateField(root, field, field, null, null, null);

        /// <param name="table">The table holding the text.</param>
        /// <param name="key">The key in that table, e.g. <c>user</c>.</param>
        /// <param name="path">The full key path, for positions, e.g. <c>examples[1].user</c>.</param>
        /// <param name="missing">The problem when the key is absent, or <see langword="null"/> when it is optional.</param>
        /// <param name="missingAt">Where to report a missing key, or <see langword="null"/> for the whole file.</param>
        /// <param name="missingMessage">The message when the key is absent.</param>
        private Template? TemplateField(TomlTable table, string key, string path, ProblemDescriptor? missing, string? missingAt, string? missingMessage)
        {
            if (!table.TryGetValue(key, out var value))
            {
                if (missing is not null)
                {
                    Add(missing, missingAt, missingMessage!);
                }

                return null;
            }

            if (value is not string text)
            {
                Add(missing == PromptProblems.MissingUser ? PromptProblems.MissingUser : PromptProblems.InvalidValue, path,
                    $"'{key}' must be text, e.g. {key} = \"...\".");
                return null;
            }

            var (trimmed, _) = Normalize(path, text);
            var (segments, errors) = TemplateParser.Parse(trimmed);
            foreach (var error in errors)
            {
                AddInText(PromptProblems.InvalidPlaceholder, path, error.Offset, error.Message);
            }

            return new Template(trimmed, segments);
        }

        private List<(ParsedExample Example, string Path)> Examples(TomlTable root)
        {
            var examples = new List<(ParsedExample, string)>();
            if (!root.TryGetValue("examples", out var value))
            {
                return examples;
            }

            var tables = value switch
            {
                TomlTableArray array => array.ToList(),
                TomlArray array when array.All(item => item is TomlTable) => array.Cast<TomlTable>().ToList(),
                _ => null,
            };

            if (tables is null)
            {
                Add(PromptProblems.InvalidValue, "examples",
                    "Write each example as its own [[examples]] section with user = \"...\" and assistant = \"...\".");
                return examples;
            }

            for (var i = 0; i < tables.Count; i++)
            {
                var path = $"examples[{i}]";
                CheckKeys(tables[i], path, ExampleFields, "in [[examples]]");
                const string Missing = "Each [[examples]] entry needs a user text and the assistant answer, e.g. user = \"...\" and assistant = \"...\".";
                var user = TemplateField(tables[i], "user", path + ".user", PromptProblems.InvalidValue, path, Missing);
                var assistant = TemplateField(tables[i], "assistant", path + ".assistant", PromptProblems.InvalidValue, path, Missing);
                if (user is not null && assistant is not null)
                {
                    examples.Add((new ParsedExample(user, assistant), path));
                }
            }

            return examples;
        }

        private List<ParsedVariable> Variables(TomlTable root)
        {
            var variables = new List<ParsedVariable>();
            if (!root.TryGetValue("variables", out var value))
            {
                return variables;
            }

            if (value is not TomlTable table)
            {
                Add(PromptProblems.InvalidValue, "variables", "Write each variable as its own section: [variables.ticket] with a type.");
                return variables;
            }

            foreach (var pair in table)
            {
                var name = pair.Key;
                var path = "variables." + name;
                if (!Identifiers.IsValidVariableName(name))
                {
                    Add(PromptProblems.InvalidVariable, path, $"'{name}' is not a valid variable name: start with a letter or _, then use letters, digits or _.");
                    continue;
                }

                if (pair.Value is not TomlTable section)
                {
                    Add(PromptProblems.InvalidVariable, path, $"Write '{name}' as its own section: [variables.{name}] with type = \"text\".");
                    continue;
                }

                CheckKeys(section, path, VariableFields, $"in [variables.{name}]");
                if (Kind(section, name, path) is not { } kind)
                {
                    continue;
                }

                var required = Bool(section, "required", path + ".required") ?? false;
                var defaultValue = Default(section, name, kind, path);
                var description = Text(section, "description", path + ".description");
                if (required && defaultValue is not null)
                {
                    Add(PromptProblems.RequiredWithDefault, path + ".default",
                        $"'{name}' is required, so its default is never used. Remove 'required' or the default.");
                }

                variables.Add(new ParsedVariable(name, kind, required, defaultValue, description?.Trim()));
            }

            return variables;
        }

        private VariableKind? Kind(TomlTable section, string name, string path)
        {
            if (!section.TryGetValue("type", out var value))
            {
                Add(PromptProblems.InvalidVariable, path, $"[variables.{name}] needs a type: type = \"text\", \"number\" or \"boolean\".");
                return null;
            }

            switch (value)
            {
                case "text":
                    return VariableKind.Text;
                case "number":
                    return VariableKind.Number;
                case "boolean":
                    return VariableKind.Boolean;
                default:
                    Add(PromptProblems.InvalidVariable, path + ".type", $"'{value}' is not a valid type. Use \"text\", \"number\" or \"boolean\".");
                    return null;
            }
        }

        private object? Default(TomlTable section, string name, VariableKind kind, string path)
        {
            if (!section.TryGetValue("default", out var value))
            {
                return null;
            }

            switch (kind, value)
            {
                case (VariableKind.Text, string text):
                    return text;
                case (VariableKind.Number, long whole):
                    return (double)whole;
                case (VariableKind.Number, double number) when !double.IsNaN(number) && !double.IsInfinity(number):
                    return number;
                case (VariableKind.Boolean, bool flag):
                    return flag;
                default:
                    var expected = kind switch
                    {
                        VariableKind.Text => "text in quotes, e.g. default = \"English\"",
                        VariableKind.Number => "a number, e.g. default = 3",
                        _ => "true or false",
                    };
                    Add(PromptProblems.InvalidVariable, path + ".default", $"The default of '{name}' must be {expected}.");
                    return null;
            }
        }

        private ParsedModelSettings? Model(TomlTable root)
        {
            if (!root.TryGetValue("model", out var value))
            {
                return null;
            }

            if (value is not TomlTable table)
            {
                Add(PromptProblems.InvalidValue, "model", "Write the model settings as a section: [model] with name = \"...\".");
                return null;
            }

            CheckKeys(table, "model", ModelFields, "in [model]");
            return new ParsedModelSettings(
                Text(table, "name", "model.name"),
                Number(table, "temperature", "model.temperature", min: 0, max: null),
                Number(table, "top_p", "model.top_p", min: 0, max: 1),
                Integer(table, "top_k", "model.top_k"),
                Integer(table, "max_output_tokens", "model.max_output_tokens"),
                TextList(table, "stop_sequences", "model.stop_sequences"),
                Effort(table));
        }

        /// <summary>Optional reasoning effort; one of a fixed list, so a typo is caught here rather than by the model provider.</summary>
        private string? Effort(TomlTable table)
        {
            var effort = Text(table, "effort", "model.effort");
            if (effort is null || Array.IndexOf(EffortLevels, effort) >= 0)
            {
                return effort;
            }

            var hint = Suggestions.Closest(effort, EffortLevels) is { } close ? $" Did you mean '{close}'?" : string.Empty;
            Add(PromptProblems.InvalidValue, "model.effort", $"'{effort}' is not a valid effort.{hint} Use one of: {string.Join(", ", EffortLevels)}.");
            return null;
        }

        private string? OutputSchema(TomlTable root)
        {
            if (!root.TryGetValue("output", out var value))
            {
                return null;
            }

            if (value is not TomlTable table)
            {
                Add(PromptProblems.InvalidValue, "output", "Write the output as a section: [output] with schema = '''{ ... }'''.");
                return null;
            }

            CheckKeys(table, "output", OutputFields, "in [output]");
            if (!table.TryGetValue("schema", out var schemaValue))
            {
                Add(PromptProblems.InvalidValue, "output", "[output] needs a schema: schema = '''{ \"type\": \"object\", \"properties\": { ... } }'''.");
                return null;
            }

            if (schemaValue is not string schema)
            {
                Add(PromptProblems.InvalidValue, "output.schema", "'schema' must be the JSON schema as text, e.g. schema = '''{ ... }'''.");
                return null;
            }

            var (trimmed, _) = Normalize("output.schema", schema);
            if (JsonChecker.Check(trimmed) is { } error)
            {
                AddInText(PromptProblems.InvalidSchema, "output.schema", error.Offset, $"The output schema is not valid JSON: {error.Message}");
            }

            return trimmed;
        }

        private List<KeyValuePair<string, string>> Metadata(TomlTable root)
        {
            var metadata = new List<KeyValuePair<string, string>>();
            if (!root.TryGetValue("metadata", out var value))
            {
                return metadata;
            }

            if (value is not TomlTable table)
            {
                Add(PromptProblems.InvalidValue, "metadata", "Write metadata as a section: [metadata] with text values, e.g. owner = \"support-team\".");
                return metadata;
            }

            foreach (var pair in table)
            {
                if (pair.Value is string text)
                {
                    metadata.Add(new KeyValuePair<string, string>(pair.Key, text));
                }
                else
                {
                    Add(PromptProblems.InvalidValue, "metadata." + pair.Key, $"Metadata '{pair.Key}' must be text in quotes.");
                }
            }

            return metadata;
        }

        private string? Text(TomlTable table, string key, string path)
        {
            if (!table.TryGetValue(key, out var value))
            {
                return null;
            }

            if (value is string text)
            {
                return text;
            }

            Add(PromptProblems.InvalidValue, path, $"'{key}' must be text in quotes.");
            return null;
        }

        private bool? Bool(TomlTable table, string key, string path)
        {
            if (!table.TryGetValue(key, out var value))
            {
                return null;
            }

            if (value is bool flag)
            {
                return flag;
            }

            Add(PromptProblems.InvalidValue, path, $"'{key}' must be true or false.");
            return null;
        }

        private double? Number(TomlTable table, string key, string path, double min, double? max)
        {
            if (!table.TryGetValue(key, out var value))
            {
                return null;
            }

            double? number = value switch
            {
                long whole => whole,
                double d when !double.IsNaN(d) && !double.IsInfinity(d) => d,
                _ => null,
            };

            var range = max is null ? $"{min} or more" : $"from {min} to {max}";
            if (number is null || number < min || number > max)
            {
                Add(PromptProblems.InvalidValue, path, $"'{key}' must be a number {range}.");
                return null;
            }

            return number;
        }

        private int? Integer(TomlTable table, string key, string path)
        {
            if (!table.TryGetValue(key, out var value))
            {
                return null;
            }

            if (value is long whole && whole >= 1 && whole <= int.MaxValue)
            {
                return (int)whole;
            }

            Add(PromptProblems.InvalidValue, path, $"'{key}' must be a whole number of 1 or more.");
            return null;
        }

        private List<string>? TextList(TomlTable table, string key, string path)
        {
            if (!table.TryGetValue(key, out var value))
            {
                return null;
            }

            if (value is TomlArray array && array.All(item => item is string))
            {
                return array.Cast<string>().ToList();
            }

            Add(PromptProblems.InvalidValue, path, $"'{key}' must be a list of text, e.g. {key} = [\"a\", \"b\"].");
            return null;
        }

        private void CheckKeys(TomlTable table, string path, string[] allowed, string where)
        {
            foreach (var pair in table)
            {
                if (Array.IndexOf(allowed, pair.Key) >= 0)
                {
                    continue;
                }

                var hint = Suggestions.Closest(pair.Key, allowed) is { } close ? $" Did you mean '{close}'?" : string.Empty;
                if (path.Length > 0 && Array.IndexOf(TopLevelFields, pair.Key) >= 0)
                {
                    hint += " " + SectionHint;
                }
                Add(PromptProblems.UnknownField, path.Length == 0 ? pair.Key : path + "." + pair.Key,
                    $"'{pair.Key}' is not a known field {where}.{hint} Known fields: {string.Join(", ", allowed)}.");
            }
        }

        /// <summary>Line endings to <c>\n</c>, trimmed; remembers how much was trimmed in front, to locate positions in the text.</summary>
        private (string Trimmed, int Lead) Normalize(string path, string text)
        {
            var lf = text.Replace("\r\n", "\n").Replace('\r', '\n');
            var lead = lf.Length - lf.TrimStart().Length;
            _texts[path] = (lf, lead);
            return (lf.Trim(), lead);
        }

        private void Add(ProblemDescriptor descriptor, string? keyPath, string message)
        {
            var position = keyPath is null ? null : index.Find(keyPath);
            problems.Add(new PromptProblem(descriptor, path, position?.Line ?? 0, position?.Column ?? 0, message));
        }

        /// <summary>Adds a problem at an offset inside a text value (a template or the schema), on its own line.</summary>
        private void AddInText(ProblemDescriptor descriptor, string keyPath, int offsetInTrimmed, string message)
        {
            var (line, column) = Locate(keyPath, offsetInTrimmed);
            problems.Add(new PromptProblem(descriptor, path, line, column, message));
        }

        /// <summary>How many <c>{{</c> start before <paramref name="end"/> (counted without overlap, like the template parser).</summary>
        private static int CountBraces(string text, int end)
        {
            var count = 0;
            for (var i = text.IndexOf("{{", StringComparison.Ordinal); i >= 0 && i < end; i = text.IndexOf("{{", i + 2, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }

        /// <summary>Where the <paramref name="n"/>-th (0-based) <c>{{</c> starts, or -1.</summary>
        private static int NthBraces(string text, int n)
        {
            var i = text.IndexOf("{{", StringComparison.Ordinal);
            for (var seen = 0; i >= 0 && seen < n; seen++)
            {
                i = text.IndexOf("{{", i + 2, StringComparison.Ordinal);
            }

            return i;
        }

        private (int Line, int Column) Locate(string keyPath, int offsetInTrimmed)
        {
            if (index.Find(keyPath) is not { } position || !_texts.TryGetValue(keyPath, out var text))
            {
                return (0, 0);
            }

            var raw = position.RawValue ?? string.Empty;
            var multiline = raw.StartsWith("\"\"\"", StringComparison.Ordinal) || raw.StartsWith("'''", StringComparison.Ordinal);
            var quote = multiline ? 3 : 1;

            // TOML drops a newline directly after an opening """ or ''', so the text starts on the next line.
            var startsOnNextLine = multiline && raw.Length > 3 && (raw[3] == '\n' || (raw[3] == '\r' && raw.Length > 4 && raw[4] == '\n'));

            var offset = Math.Min(text.Lead + offsetInTrimmed, text.Value.Length);
            if (raw.Length > 0 && string.CompareOrdinal(text.Value, offset, "{{", 0, 2) == 0)
            {
                var rawIndex = NthBraces(raw, CountBraces(text.Value, offset));
                if (rawIndex >= 0)
                {
                    var rawNewlines = 0;
                    var lastRawNewline = -1;
                    for (var i = 0; i < rawIndex; i++)
                    {
                        if (raw[i] == '\n')
                        {
                            rawNewlines++;
                            lastRawNewline = i;
                        }
                    }

                    return rawNewlines == 0
                        ? (position.ValueLine, position.ValueColumn + rawIndex)
                        : (position.ValueLine + rawNewlines, rawIndex - lastRawNewline);
                }
            }

            var newlines = 0;
            var lastNewline = -1;
            for (var i = 0; i < offset; i++)
            {
                if (text.Value[i] == '\n')
                {
                    newlines++;
                    lastNewline = i;
                }
            }

            if (startsOnNextLine)
            {
                return (position.ValueLine + 1 + newlines, offset - lastNewline);
            }

            return newlines == 0
                ? (position.ValueLine, position.ValueColumn + quote + offset)
                : (position.ValueLine + newlines, offset - lastNewline);
        }
    }
}
