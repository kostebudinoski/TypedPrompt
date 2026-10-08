using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using TypedPrompt.Core;

namespace TypedPrompt.Generator;

/// <summary>Turns Core's language-neutral problems into compiler diagnostics at the file and line.</summary>
internal static class Diagnostics
{
    private const string Category = "TypedPrompt";

    private static readonly Dictionary<string, DiagnosticDescriptor> Descriptors = PromptProblems.All.ToDictionary(
        d => d.Id,
        d => new DiagnosticDescriptor(
            d.Id,
            d.Title,
            "{0}",
            Category,
            d.Severity == ProblemSeverity.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
            isEnabledByDefault: true),
        StringComparer.Ordinal);

    public static Diagnostic Create(PromptProblem problem)
    {
        var position = problem.Line > 0
            ? new LinePosition(problem.Line - 1, Math.Max(problem.Column, 1) - 1)
            : new LinePosition(0, 0);
        var location = Location.Create(problem.Path, new TextSpan(0, 0), new LinePositionSpan(position, position));
        return Diagnostic.Create(Descriptors[problem.Id], location, problem.Message);
    }
}
