namespace TypedPrompt.Core;

/// <summary>Every kind of problem a prompt file can have. Ids are stable: never change or reuse one, add new ones.</summary>
public static class PromptProblems
{
    /// <summary>TP001: the file is not valid TOML.</summary>
    public static readonly ProblemDescriptor InvalidToml = new("TP001", "Invalid TOML", ProblemSeverity.Error);

    /// <summary>TP002: a field is not part of the format, usually a typo.</summary>
    public static readonly ProblemDescriptor UnknownField = new("TP002", "Unknown field", ProblemSeverity.Error);

    /// <summary>TP003: the required <c>user</c> text is missing or is not text.</summary>
    public static readonly ProblemDescriptor MissingUser = new("TP003", "Missing user text", ProblemSeverity.Error);

    /// <summary>TP004: a <c>{{placeholder}}</c> has no <c>[variables.&lt;name&gt;]</c> section.</summary>
    public static readonly ProblemDescriptor UndeclaredPlaceholder = new("TP004", "Undeclared placeholder", ProblemSeverity.Error);

    /// <summary>TP005: a variable is declared but no template uses it.</summary>
    public static readonly ProblemDescriptor UnusedVariable = new("TP005", "Unused variable", ProblemSeverity.Warning);

    /// <summary>TP006: a variable has an invalid name or type, or a default of the wrong type.</summary>
    public static readonly ProblemDescriptor InvalidVariable = new("TP006", "Invalid variable", ProblemSeverity.Error);

    /// <summary>TP007: a variable is required and also has a default, which is never used.</summary>
    public static readonly ProblemDescriptor RequiredWithDefault = new("TP007", "Required variable with a default", ProblemSeverity.Warning);

    /// <summary>TP008: two files produce the same class name or the same key.</summary>
    public static readonly ProblemDescriptor Duplicate = new("TP008", "Duplicate prompt", ProblemSeverity.Error);

    /// <summary>TP009: a <c>{{</c> is never closed, or a placeholder name is not valid.</summary>
    public static readonly ProblemDescriptor InvalidPlaceholder = new("TP009", "Invalid placeholder", ProblemSeverity.Error);

    /// <summary>TP010: the <c>[output]</c> schema is not well-formed JSON.</summary>
    public static readonly ProblemDescriptor InvalidSchema = new("TP010", "Invalid output schema", ProblemSeverity.Error);

    /// <summary>TP011: a field has the wrong type or a value out of range.</summary>
    public static readonly ProblemDescriptor InvalidValue = new("TP011", "Invalid value", ProblemSeverity.Error);

    /// <summary>TP012: the file name cannot be turned into a class name.</summary>
    public static readonly ProblemDescriptor InvalidFileName = new("TP012", "Invalid file name", ProblemSeverity.Error);

    /// <summary>TP013: variable names clash in the generated code, e.g. <c>max_tokens</c> and <c>maxTokens</c>, or <c>render</c>.</summary>
    public static readonly ProblemDescriptor NameConflict = new("TP013", "Generated name conflict", ProblemSeverity.Error);

    /// <summary>All descriptors, in id order.</summary>
    public static IReadOnlyList<ProblemDescriptor> All { get; } =
    [
        InvalidToml, UnknownField, MissingUser, UndeclaredPlaceholder, UnusedVariable, InvalidVariable,
        RequiredWithDefault, Duplicate, InvalidPlaceholder, InvalidSchema, InvalidValue, InvalidFileName, NameConflict,
    ];
}
