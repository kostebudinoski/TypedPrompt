namespace TypedPrompt.Core;

/// <summary>How F# code is generated.</summary>
/// <param name="Namespace">Namespace of the generated types, e.g. <c>MyApp.Prompts</c>.</param>
/// <param name="ClassSuffix">Added to the PascalCase file name: <c>ticket-summarize</c> → <c>TicketSummarizePrompt</c>.</param>
/// <param name="GeneratorVersion">Written into the <c>[&lt;GeneratedCode&gt;]</c> attribute.</param>
public sealed record FSharpEmitOptions(string Namespace, string ClassSuffix = "Prompt", string GeneratorVersion = "0.1.0");
