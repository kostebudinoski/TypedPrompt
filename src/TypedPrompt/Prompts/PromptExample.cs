namespace TypedPrompt;

/// <summary>
/// One few-shot example from <c>[[examples]]</c>: a user message and the answer the model should give. Examples are sent
/// before the real request, to show the model what a good answer looks like. In a <see cref="PromptDefinition"/> the texts
/// are templates; in a <see cref="RenderedPrompt"/> their placeholders are filled in.
/// </summary>
/// <param name="User">The example request.</param>
/// <param name="Assistant">The example answer.</param>
public sealed record PromptExample(string User, string Assistant);
