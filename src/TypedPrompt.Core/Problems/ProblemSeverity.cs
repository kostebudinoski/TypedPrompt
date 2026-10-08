namespace TypedPrompt.Core;

/// <summary>How serious a problem in a prompt file is.</summary>
public enum ProblemSeverity
{
    /// <summary>The file is rejected: no code is generated for it, and the build fails.</summary>
    Error,

    /// <summary>The file is accepted, but something is probably a mistake.</summary>
    Warning,
}
