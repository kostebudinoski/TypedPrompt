namespace TypedPrompt.Core;

/// <summary>A problem in a template's placeholder syntax.</summary>
/// <param name="Offset">Where the problem starts in the template text.</param>
/// <param name="Message">What is wrong.</param>
public sealed record TemplateError(int Offset, string Message);
