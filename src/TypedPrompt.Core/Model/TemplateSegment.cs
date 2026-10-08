namespace TypedPrompt.Core;

/// <summary>A piece of a template: literal text, or a placeholder to fill with a variable's value.</summary>
/// <param name="Kind">Text or placeholder.</param>
/// <param name="Value">The literal text (with <c>\{{</c> already turned into <c>{{</c>), or the variable name.</param>
/// <param name="Offset">Where the piece starts in <see cref="Template.Text"/>.</param>
public sealed record TemplateSegment(TemplateSegmentKind Kind, string Value, int Offset)
{
    /// <summary>A literal piece.</summary>
    public static TemplateSegment Literal(string text, int offset) => new(TemplateSegmentKind.Text, text, offset);

    /// <summary>A placeholder for <paramref name="variable"/>.</summary>
    public static TemplateSegment Placeholder(string variable, int offset) => new(TemplateSegmentKind.Placeholder, variable, offset);
}
