namespace TypedPrompt.Core;

/// <summary>A kind of problem: a stable id, a short title, and its severity. The generator turns each into a compiler diagnostic.</summary>
/// <param name="Id">Stable id, e.g. <c>TP004</c>. Never changed or reused.</param>
/// <param name="Title">Short description, e.g. "Undeclared placeholder".</param>
/// <param name="Severity">Error or warning.</param>
public sealed record ProblemDescriptor(string Id, string Title, ProblemSeverity Severity);
