namespace TypedPrompt.Core;

/// <summary>A variable from a <c>[variables.&lt;name&gt;]</c> section.</summary>
/// <param name="Name">The name used in placeholders, e.g. <c>max_sentences</c>. Emitters turn it into their own naming style.</param>
/// <param name="Kind">Text, number or boolean.</param>
/// <param name="Required">Whether a value must always be given.</param>
/// <param name="Default">The default: a <see cref="string"/>, <see cref="double"/> or <see cref="bool"/> matching <paramref name="Kind"/>; <see langword="null"/> when none.</param>
/// <param name="Description">What the variable is for.</param>
public sealed record ParsedVariable(string Name, VariableKind Kind, bool Required, object? Default, string? Description);
