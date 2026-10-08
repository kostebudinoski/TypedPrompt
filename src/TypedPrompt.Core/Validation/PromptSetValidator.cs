namespace TypedPrompt.Core;

/// <summary>Checks a whole set of prompt files together: problems no single file can see.</summary>
public static class PromptSetValidator
{
    /// <summary>Finds files that would generate the same class or share a key; each such file gets a TP008 error.</summary>
    public static IReadOnlyList<PromptProblem> Validate(IEnumerable<ParsedPrompt> prompts)
    {
        var all = prompts?.ToList() ?? [];
        var problems = new List<PromptProblem>();
        
        foreach (var group in all.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            var files = string.Join(", ", group.Select(p => System.IO.Path.GetFileName(p.Path)));
            var names = string.Join(" and ", group.Select(p => p.Name).Distinct(StringComparer.Ordinal).Select(n => $"'{n}'"));
            var caseNote = group.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() > 1
                ? " (names that differ only in upper/lower case clash too, because generated file names ignore case)"
                : string.Empty;
            problems.AddRange(group.Select(p => new PromptProblem(PromptProblems.Duplicate, p.Path, 0, 0,
                $"The class name {names} comes from more than one file: {files}{caseNote}. Rename one of the files.")));
        }

        foreach (var group in all.GroupBy(p => p.Key, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            var files = string.Join(", ", group.Select(p => System.IO.Path.GetFileName(p.Path)));
            problems.AddRange(group.Select(p => new PromptProblem(PromptProblems.Duplicate, p.Path, 0, 0,
                $"The key '{group.Key}' is used by more than one file: {files}. Keys must be unique.")));
        }

        return problems;
    }
}
