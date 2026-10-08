namespace TypedPrompt;

/// <summary>
/// A prompt with its variable values: what every generated prompt class is. The values are set in the constructor
/// (required variables are required parameters), so <see cref="Render"/> needs no arguments and code can render any
/// prompt without knowing its variables.
/// </summary>
public interface ITypedPrompt
{
    /// <summary>What the prompt file defines: key, templates, model settings, output schema, variables, metadata.</summary>
    PromptDefinition Definition { get; }

    /// <summary>Fills every placeholder with this instance's values.</summary>
    RenderedPrompt Render();
}
