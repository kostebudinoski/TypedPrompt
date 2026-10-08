namespace TypedPrompt;

/// <summary>
/// The model settings the prompt was written for, from the file's <c>[model]</c> section. Advisory: they travel with the
/// prompt so callers can apply them, but nothing here calls a model.
/// </summary>
/// <param name="Name">Model name, e.g. <c>claude-sonnet-5</c>.</param>
/// <param name="Temperature">Sampling temperature.</param>
/// <param name="TopP">Nucleus sampling share.</param>
/// <param name="TopK">Top-k sampling.</param>
/// <param name="MaxOutputTokens">Maximum number of output tokens (output only, not input).</param>
/// <param name="StopSequences">Texts that stop generation.</param>
/// <param name="Effort">
/// Reasoning effort the prompt was tuned for: <c>minimal</c>, <c>low</c>, <c>medium</c>, <c>high</c> or <c>max</c>.
/// Map it to your SDK's setting (e.g. a reasoning-effort option or a thinking budget).
/// </param>
public sealed record ModelSettings(
    string? Name = null,
    double? Temperature = null,
    double? TopP = null,
    int? TopK = null,
    int? MaxOutputTokens = null,
    IReadOnlyList<string>? StopSequences = null,
    string? Effort = null);
