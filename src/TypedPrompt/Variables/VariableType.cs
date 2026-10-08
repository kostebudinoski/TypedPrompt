namespace TypedPrompt;

/// <summary>The type of a prompt variable, from its <c>type</c> field. Decides the C# parameter type.</summary>
public enum VariableType
{
    /// <summary><c>text</c>: a <see cref="string"/> parameter.</summary>
    Text,

    /// <summary><c>number</c>: a <see cref="double"/> parameter.</summary>
    Number,

    /// <summary><c>boolean</c>: a <see cref="bool"/> parameter.</summary>
    Boolean,
}
