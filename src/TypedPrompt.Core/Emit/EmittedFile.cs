namespace TypedPrompt.Core;

/// <summary>One generated source file.</summary>
/// <param name="Name">File name, e.g. <c>TicketSummarizePrompt.g.cs</c>.</param>
/// <param name="Content">The source code.</param>
public sealed record EmittedFile(string Name, string Content);
