namespace TypedPrompt.Core;

/// <summary>How C# code is generated.</summary>
/// <param name="Namespace">Namespace of the generated classes, e.g. <c>MyApp.Prompts</c>.</param>
/// <param name="ClassSuffix">Added to the PascalCase file name: <c>ticket-summarize</c> → <c>TicketSummarizePrompt</c>.</param>
/// <param name="GeneratorVersion">Written into the <c>[GeneratedCode]</c> attribute.</param>
public sealed record CSharpEmitOptions(string Namespace, string ClassSuffix = "Prompt", string GeneratorVersion = "0.1.0");
