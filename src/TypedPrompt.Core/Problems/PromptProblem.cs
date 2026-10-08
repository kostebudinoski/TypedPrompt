using System.Globalization;

namespace TypedPrompt.Core;

/// <summary>One problem in a prompt file, located by file, line and column.</summary>
/// <param name="Descriptor">What kind of problem it is.</param>
/// <param name="Path">The file.</param>
/// <param name="Line">1-based line, or 0 when unknown.</param>
/// <param name="Column">1-based column, or 0 when unknown.</param>
/// <param name="Message">What is wrong, with a hint where possible.</param>
public sealed record PromptProblem(ProblemDescriptor Descriptor, string Path, int Line, int Column, string Message)
{
    /// <summary>The stable id, e.g. <c>TP004</c>.</summary>
    public string Id => Descriptor.Id;

    /// <summary>Error or warning.</summary>
    public ProblemSeverity Severity => Descriptor.Severity;

    /// <summary>Formats like a compiler message: <c>ticket.prompt.toml(12,5): error TP004: ...</c>.</summary>
    public override string ToString()
    {
        var location = Line > 0 ? string.Format(CultureInfo.InvariantCulture, "({0},{1})", Line, Math.Max(Column, 1)) : string.Empty;
        var severity = Severity == ProblemSeverity.Error ? "error" : "warning";
        return $"{Path}{location}: {severity} {Id}: {Message}";
    }
}
