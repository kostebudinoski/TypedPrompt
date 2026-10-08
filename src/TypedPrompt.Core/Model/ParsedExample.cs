namespace TypedPrompt.Core;

/// <summary>One <c>[[examples]]</c> entry: a few-shot user message and the answer the model should give.</summary>
/// <param name="User">The example request.</param>
/// <param name="Assistant">The example answer.</param>
public sealed record ParsedExample(Template User, Template Assistant);
