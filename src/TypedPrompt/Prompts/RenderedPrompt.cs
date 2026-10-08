namespace TypedPrompt;

/// <summary>A prompt with every placeholder filled in: what you send to a model, plus the settings that go with it.</summary>
/// <param name="Key">The prompt's key, e.g. <c>ticket-summarize</c>.</param>
/// <param name="System">The rendered system text, or <see langword="null"/> when the prompt has none.</param>
/// <param name="User">The rendered user text: the real request.</param>
/// <param name="Model">The model settings the prompt was written for, if any.</param>
/// <param name="OutputSchema">The JSON schema the answer must follow, or <see langword="null"/> for a plain-text answer.</param>
public sealed record RenderedPrompt(string Key, string? System, string User, ModelSettings? Model = null, string? OutputSchema = null)
{
    /// <summary>
    /// Which exact prompt produced this: <c>key@label#hash8</c> (see <see cref="PromptDefinition.Version"/>). Log it with
    /// every model call to know which prompt version was behind an output.
    /// </summary>
    public string Version { get; init; } = Key;

    /// <summary>The rendered few-shot examples, in file order; sent between the system text and the user text.</summary>
    public IReadOnlyList<PromptExample> Examples { get; init; } = [];

    /// <summary>
    /// The messages in order, as chat APIs expect them: the system message (when there is one), then each example as a
    /// user and an assistant message, then the user message.
    /// </summary>
    public IReadOnlyList<PromptMessage> Messages
    {
        get
        {
            var messages = new List<PromptMessage>(2 + (Examples.Count * 2));
            if (System is not null)
            {
                messages.Add(new PromptMessage(PromptRole.System, System));
            }

            foreach (var example in Examples)
            {
                messages.Add(new PromptMessage(PromptRole.User, example.User));
                messages.Add(new PromptMessage(PromptRole.Assistant, example.Assistant));
            }

            messages.Add(new PromptMessage(PromptRole.User, User));
            return messages;
        }
    }

    /// <summary>All messages as one text, separated by a blank line: for models or logs that take a single string.</summary>
    public string Text => string.Join("\n\n", Messages.Select(m => m.Text));
}
