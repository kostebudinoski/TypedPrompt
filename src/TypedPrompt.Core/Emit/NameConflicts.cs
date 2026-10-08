namespace TypedPrompt.Core;

/// <summary>Finds variables whose generated member names clash (TP013). Shared by every emitter that turns variables into members.</summary>
internal static class NameConflicts
{
    /// <summary>Members every generated class has; a variable must not produce one of these names.</summary>
    public static readonly string[] ReservedMembers = ["Definition", "Render", "Equals", "GetHashCode", "ToString", "GetType", "MemberwiseClone", "Finalize"];

    /// <param name="prompt">The prompt.</param>
    /// <param name="className">The generated class name.</param>
    /// <param name="memberName">How the target language names a variable's member, e.g. <c>max_tokens</c> → <c>MaxTokens</c>.</param>
    /// <param name="language">Shown in messages, e.g. "C#".</param>
    public static List<PromptProblem> Find(ParsedPrompt prompt, string className, Func<string, string> memberName, string language)
    {
        var problems = new List<PromptProblem>();
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var variable in prompt.Variables)
        {
            var member = memberName(variable.Name);
            if (seen.TryGetValue(member, out var other))
            {
                problems.Add(Conflict(prompt, $"Variables '{other}' and '{variable.Name}' both become the {language} property '{member}'. Rename one of them."));
            }
            else if (Array.IndexOf(ReservedMembers, member) >= 0 || member == className)
            {
                problems.Add(Conflict(prompt, $"Variable '{variable.Name}' would become the {language} property '{member}', which the generated class already uses. Rename the variable."));
            }

            seen[member] = variable.Name;
        }

        return problems;
    }

    private static PromptProblem Conflict(ParsedPrompt prompt, string message) =>
        new(PromptProblems.NameConflict, prompt.Path, 0, 0, message);
}
