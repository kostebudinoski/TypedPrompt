namespace TypedPrompt.Core;

/// <summary>
/// One valid prompt file, read into a model that does not depend on any target language. Every emitter (C# now,
/// TypeScript later) generates code from this.
/// </summary>
/// <param name="Path">The file, for messages.</param>
/// <param name="Key">The <c>key</c> field, or the file name without <c>.prompt.toml</c>.</param>
/// <param name="Name">The file name as PascalCase words, e.g. <c>TicketSummarize</c>; emitters add their own suffix (<c>Prompt</c>).</param>
/// <param name="Description">What the prompt is for.</param>
/// <param name="Tags">Free tags, in file order.</param>
/// <param name="Metadata">Free text values from <c>[metadata]</c>, in file order.</param>
/// <param name="System">The system template, or <see langword="null"/>.</param>
/// <param name="User">The user template.</param>
/// <param name="Examples">Few-shot examples from <c>[[examples]]</c>, in file order; sent between system and user.</param>
/// <param name="Variables">The declared variables, in file order.</param>
/// <param name="Model">The <c>[model]</c> settings, or <see langword="null"/>.</param>
/// <param name="OutputSchema">The <c>[output]</c> JSON schema text (trimmed), or <see langword="null"/>.</param>
/// <param name="VersionLabel">The optional <c>version</c> field: a human label such as <c>2.1</c>. The content hash identifies the prompt either way.</param>
public sealed record ParsedPrompt(
    string Path,
    string Key,
    string Name,
    string? Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<KeyValuePair<string, string>> Metadata,
    Template? System,
    Template User,
    IReadOnlyList<ParsedExample> Examples,
    IReadOnlyList<ParsedVariable> Variables,
    ParsedModelSettings? Model,
    string? OutputSchema,
    string? VersionLabel = null)
{
    /// <summary>The content hash (<see cref="PromptHasher"/>): 64 lowercase hex characters.</summary>
    public string ContentHash => PromptHasher.Compute(this);

    /// <summary>
    /// The identifier to log with every output: <c>key@label#hash8</c>, or <c>key#hash8</c> without a label, e.g.
    /// <c>ticket-summarize@2.1#9bd64237</c>.
    /// </summary>
    public string Version
    {
        get
        {
            var hash = ContentHash.Substring(0, 8);
            return VersionLabel is null ? $"{Key}#{hash}" : $"{Key}@{VersionLabel}#{hash}";
        }
    }
}
