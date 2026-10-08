namespace TypedPrompt.Core;

/// <summary>Where a key and its value are written in a file (1-based lines and columns).</summary>
/// <param name="Line">Line of the key.</param>
/// <param name="Column">Column of the key.</param>
/// <param name="ValueLine">Line where the value starts.</param>
/// <param name="ValueColumn">Column where the value starts.</param>
/// <param name="RawValue">The value as written, including quotes (for strings), used to locate positions inside text.</param>
internal sealed record SourcePosition(int Line, int Column, int ValueLine, int ValueColumn, string? RawValue);
