namespace TypedPrompt.Core;

/// <summary>What a <see cref="TemplateSegment"/> is.</summary>
public enum TemplateSegmentKind
{
    /// <summary>Literal text, copied as it is.</summary>
    Text,

    /// <summary>A <c>{{name}}</c> placeholder, filled with the variable's value.</summary>
    Placeholder,
}
