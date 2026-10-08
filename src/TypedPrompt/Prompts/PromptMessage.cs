namespace TypedPrompt;

/// <summary>One rendered message, ready to send to a model.</summary>
/// <param name="Role">System or user.</param>
/// <param name="Text">The text with every placeholder filled in.</param>
public sealed record PromptMessage(PromptRole Role, string Text);
