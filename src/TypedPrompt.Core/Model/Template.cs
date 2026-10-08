namespace TypedPrompt.Core;

/// <summary>A <c>system</c> or <c>user</c> text, already split into literal text and placeholders at build time.</summary>
/// <param name="Text">The template as written (line endings normalised to <c>\n</c>, trimmed at both ends).</param>
/// <param name="Segments">Literal text and placeholders, in order. Rendering is: concatenate them, filling placeholders.</param>
public sealed record Template(string Text, IReadOnlyList<TemplateSegment> Segments);
