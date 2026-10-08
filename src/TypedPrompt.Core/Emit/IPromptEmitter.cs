namespace TypedPrompt.Core;

/// <summary>
/// Generates code in one target language from a <see cref="ParsedPrompt"/>. C# is the first; adding a language (e.g.
/// TypeScript) means adding an emitter, and a front end that writes its files (the Roslyn generator only handles C#).
/// </summary>
public interface IPromptEmitter
{
    /// <summary>The target language, e.g. <c>csharp</c>.</summary>
    string Language { get; }

    /// <summary>Generates the files for one prompt.</summary>
    EmitResult Emit(ParsedPrompt prompt);
}
