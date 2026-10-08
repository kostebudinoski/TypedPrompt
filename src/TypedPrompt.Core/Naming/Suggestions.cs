namespace TypedPrompt.Core;

/// <summary>"Did you mean ...?" for typos: the closest known name within two edits.</summary>
internal static class Suggestions
{
    public static string? Closest(string name, IEnumerable<string> known) =>
        known
            .Select(candidate => (Candidate: candidate, Distance: Distance(name, candidate)))
            .Where(c => c.Distance <= 2 && c.Distance < Math.Max(name.Length, 1))
            .OrderBy(c => c.Distance)
            .ThenBy(c => c.Candidate, StringComparer.Ordinal)
            .Select(c => c.Candidate)
            .FirstOrDefault();

    /// <summary>Levenshtein distance, case-insensitive.</summary>
    private static int Distance(string a, string b)
    {
        a = a.ToUpperInvariant();
        b = b.ToUpperInvariant();
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1];
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            previous = current;
        }

        return previous[b.Length];
    }
}
