namespace TypedPrompt;

/// <summary>A variable the prompt declares, from a <c>[variables.&lt;name&gt;]</c> section.</summary>
/// <param name="Name">The name used in <c>{{name}}</c> placeholders.</param>
/// <param name="Type">Text, number or boolean.</param>
/// <param name="Required">Whether a value must be passed (a required parameter of <c>Render</c>).</param>
/// <param name="Default">The value used when none is passed: a <see cref="string"/>, <see cref="double"/> or <see cref="bool"/>.</param>
/// <param name="Description">What the variable is for; also the XML doc of the parameter.</param>
public sealed record VariableInfo(string Name, VariableType Type, bool Required, object? Default = null, string? Description = null);
