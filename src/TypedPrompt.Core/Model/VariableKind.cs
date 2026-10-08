namespace TypedPrompt.Core;

/// <summary>The type of a variable. File names: <c>text</c>, <c>number</c>, <c>boolean</c>.</summary>
public enum VariableKind
{
    /// <summary><c>text</c></summary>
    Text,

    /// <summary><c>number</c></summary>
    Number,

    /// <summary><c>boolean</c></summary>
    Boolean,
}
