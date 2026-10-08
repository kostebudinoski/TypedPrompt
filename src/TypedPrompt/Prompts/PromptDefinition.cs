using System.Collections.ObjectModel;

namespace TypedPrompt;

/// <summary>
/// Everything a <c>*.prompt.toml</c> file defines, independent of any values: one per prompt class, available as its
/// static <c>Definition</c> and through <see cref="ITypedPrompt.Definition"/>.
/// </summary>
/// <param name="Key">The prompt's key: the file name without <c>.prompt.toml</c>, or the file's <c>key</c> field.</param>
/// <param name="UserTemplate">The user text with its <c>{{placeholders}}</c>.</param>
public sealed record PromptDefinition(string Key, string UserTemplate)
{
    private static readonly IReadOnlyDictionary<string, string> NoMetadata = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    /// <summary>
    /// SHA-256 (lowercase hex) of everything that changes the model's behaviour (texts, examples, variables, model
    /// settings, schema), computed at build time. Changes whenever the prompt changes, and only then.
    /// </summary>
    public string ContentHash { get; init; } = string.Empty;

    /// <summary>The optional <c>version</c> field from the file: a human label such as <c>2.1</c>.</summary>
    public string? VersionLabel { get; init; }

    /// <summary>
    /// The identifier to log with every output: <c>key@label#hash8</c>, or <c>key#hash8</c> without a label, e.g.
    /// <c>ticket-summarize@2.1#9bd64237</c>. The hash part is authoritative; the label is for people.
    /// </summary>
    public string Version
    {
        get
        {
            var hash = ContentHash.Length > 8 ? ContentHash.Substring(0, 8) : ContentHash;
            var label = VersionLabel is null ? string.Empty : "@" + VersionLabel;
            return hash.Length == 0 ? Key + label : $"{Key}{label}#{hash}";
        }
    }

    /// <summary>The system text with its <c>{{placeholders}}</c>, or <see langword="null"/>.</summary>
    public string? SystemTemplate { get; init; }

    /// <summary>Few-shot examples from <c>[[examples]]</c>, as templates, in file order.</summary>
    public IReadOnlyList<PromptExample> Examples { get; init; } = [];

    /// <summary>What the prompt is for.</summary>
    public string? Description { get; init; }

    /// <summary>Free tags for grouping and search.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Free text values from the file's <c>[metadata]</c> section, e.g. an owner.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = NoMetadata;

    /// <summary>The model settings the prompt was written for, or <see langword="null"/>.</summary>
    public ModelSettings? Model { get; init; }

    /// <summary>The JSON schema the answer must follow, or <see langword="null"/> for a plain-text answer.</summary>
    public string? OutputSchema { get; init; }

    /// <summary>The declared variables, in file order.</summary>
    public IReadOnlyList<VariableInfo> Variables { get; init; } = [];
}
