namespace TypedPrompt.Core;

/// <summary>The result of reading one prompt file.</summary>
/// <param name="Prompt">The prompt, or <see langword="null"/> when the file has errors.</param>
/// <param name="Problems">Every error and warning, in file order.</param>
public sealed record PromptParseResult(ParsedPrompt? Prompt, IReadOnlyList<PromptProblem> Problems)
{
    /// <summary>Whether any problem is an error.</summary>
    public bool HasErrors => Problems.Any(p => p.Severity == ProblemSeverity.Error);
}
