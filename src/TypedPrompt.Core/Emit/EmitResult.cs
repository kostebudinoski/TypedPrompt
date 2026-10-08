namespace TypedPrompt.Core;

/// <summary>What an emitter produced for one prompt: files, or problems that stopped it (e.g. names that clash in that language).</summary>
/// <param name="Files">The generated files; empty when there are errors.</param>
/// <param name="Problems">Language-specific problems.</param>
public sealed record EmitResult(IReadOnlyList<EmittedFile> Files, IReadOnlyList<PromptProblem> Problems);
