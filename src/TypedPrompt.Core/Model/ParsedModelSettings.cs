namespace TypedPrompt.Core;

/// <summary>The <c>[model]</c> section.</summary>
/// <param name="Name">Model name.</param>
/// <param name="Temperature">Sampling temperature, 0 or more.</param>
/// <param name="TopP">Nucleus sampling share, 0 to 1.</param>
/// <param name="TopK">Top-k sampling, 1 or more.</param>
/// <param name="MaxOutputTokens">Maximum output tokens, 1 or more.</param>
/// <param name="StopSequences">Texts that stop generation.</param>
/// <param name="Effort">Reasoning effort: <c>minimal</c>, <c>low</c>, <c>medium</c>, <c>high</c> or <c>max</c>; <see langword="null"/> when not set.</param>
public sealed record ParsedModelSettings(
    string? Name,
    double? Temperature,
    double? TopP,
    int? TopK,
    int? MaxOutputTokens,
    IReadOnlyList<string>? StopSequences,
    string? Effort = null);
