namespace TypedPrompt.Core.Tests;

public class HelperTests
{
    [Theory]
    [InlineData("""{"type":"object","properties":{"a":{"type":"string"}},"required":["a"]}""")]
    [InlineData(" [1, -2.5e+3, true, false, null, \"\\u00e9\\n\"] ")]
    [InlineData("0")]
    public void Well_formed_json_passes(string json) =>
        Assert.Null(JsonChecker.Check(json));

    [Theory]
    [InlineData("""{"a": 1,}""", "Expected a property name")]
    [InlineData("""{'a': 1}""", "Expected a property name")]
    [InlineData("""{"a": 1""", "ends")]
    [InlineData("""[01]""", "Expected ']'")]
    [InlineData("""{"a": tru}""", "Unexpected")]
    [InlineData("""{} {}""", "after the JSON value")]
    [InlineData("", "ends too early")]
    public void Malformed_json_says_what_is_wrong(string json, string expected)
    {
        var error = JsonChecker.Check(json);

        Assert.NotNull(error);
        Assert.Contains(expected, error.Value.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ticket-summarize", "TicketSummarize")]
    [InlineData("support_reply.v2", "SupportReplyV2")]
    [InlineData("greeting", "Greeting")]
    [InlineData("maxTokens", "MaxTokens")]
    [InlineData("123", "")]
    [InlineData("--", "")]
    public void File_names_become_pascal_case(string stem, string expected) =>
        Assert.Equal(expected, Identifiers.ToPascalCase(stem));

    [Theory]
    [InlineData("prompts/ticket-summarize.prompt.toml", "ticket-summarize")]
    [InlineData(@"C:\app\Prompts\Greeting.PROMPT.TOML", "Greeting")]
    [InlineData("other.toml", "other")]
    public void File_stem_drops_the_prompt_extension(string path, string expected) =>
        Assert.Equal(expected, Identifiers.FileStem(path));

    [Theory]
    [InlineData("ticket", true)]
    [InlineData("_private", true)]
    [InlineData("max_sentences2", true)]
    [InlineData("2fast", false)]
    [InlineData("bad-name", false)]
    [InlineData("naïve", false)]
    [InlineData("", false)]
    public void Variable_names_are_ascii_identifiers(string name, bool valid) =>
        Assert.Equal(valid, Identifiers.IsValidVariableName(name));

    [Fact]
    public void Every_problem_id_is_unique_and_listed()
    {
        Assert.Equal(13, PromptProblems.All.Count);
        Assert.Equal(PromptProblems.All.Count, PromptProblems.All.Select(d => d.Id).Distinct().Count());
        Assert.All(PromptProblems.All, d => Assert.Matches("^TP0[0-9]{2}$", d.Id));
    }
}
