namespace TypedPrompt.Core.Tests;

public class FSharpEmitterTests
{
    private static readonly FSharpEmitter Emitter = new(new FSharpEmitOptions("MyApp.Prompts"));

    [Fact]
    public void One_file_per_prompt_named_after_the_class()
    {
        var file = Assert.Single(Emit("ticket-summarize.prompt.toml", "user = \"{{ticket}}\"\n[variables.ticket]\ntype = \"text\"\nrequired = true\n").Files);

        Assert.Equal("TicketSummarizePrompt.g.fs", file.Name);
        Assert.Contains("namespace MyApp.Prompts", file.Content, StringComparison.Ordinal);
        Assert.Contains("type TicketSummarizePrompt(ticket: string) =", file.Content, StringComparison.Ordinal);
        Assert.Contains("interface ITypedPrompt with", file.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Required_parameters_come_first_and_optional_ones_use_fsharp_optional_arguments()
    {
        var content = Content("""
            user = "{{tone}} {{name}} {{count}} {{loud}} {{nick}}"

            [variables.tone]
            type    = "text"
            default = "friendly"

            [variables.name]
            type     = "text"
            required = true

            [variables.count]
            type    = "number"
            default = 2

            [variables.loud]
            type    = "boolean"
            default = true

            [variables.nick]
            type = "text"
            """);

        Assert.Contains("type TestPrompt(name: string, ?tone: string, ?count: float, ?loud: bool, ?nick: string) =", content, StringComparison.Ordinal);
        Assert.Contains("let tone = defaultArg tone \"friendly\"", content, StringComparison.Ordinal);
        Assert.Contains("let count = defaultArg count 2.0", content, StringComparison.Ordinal);
        Assert.Contains("let loud = defaultArg loud true", content, StringComparison.Ordinal);
        Assert.DoesNotContain("defaultArg nick", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Optional_values_without_default_stay_options_and_render_as_empty()
    {
        var content = Content("user = \"[{{nick}}] [{{age}}]\"\n[variables.nick]\ntype = \"text\"\n[variables.age]\ntype = \"number\"\n");

        Assert.Contains("member _.Nick = nick", content, StringComparison.Ordinal);
        Assert.Contains("PromptValue.Format(Option.toObj nick)", content, StringComparison.Ordinal);
        Assert.Contains("PromptValue.Format(Option.toNullable age)", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Keywords_get_double_backticks()
    {
        var content = Content("user = \"{{type}}\"\n[variables.type]\ntype = \"text\"\nrequired = true\n");

        Assert.Contains("type TestPrompt(``type``: string) =", content, StringComparison.Ordinal);
        Assert.Contains("member _.Type = ``type``", content, StringComparison.Ordinal);
        Assert.Contains("/// <param name=\"type\">", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Definition_uses_fsharp_collections_and_nullable_model_settings()
    {
        var content = Content("""
            user = "Hi"
            tags = ["a", "b"]

            [model]
            name        = "m"
            temperature = 1
            top_k       = 40
            effort      = "high"

            [metadata]
            owner = "team"
            """);

        Assert.Contains("Tags = ([| \"a\"; \"b\" |] :> IReadOnlyList<string>)", content, StringComparison.Ordinal);
        Assert.Contains("Metadata = readOnlyDict [ (\"owner\", \"team\") ]", content, StringComparison.Ordinal);
        Assert.Contains("Model = ModelSettings(Name = \"m\", Temperature = Nullable 1.0, TopK = Nullable 40, Effort = \"high\")", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Examples_are_in_the_definition_and_rendered_with_their_placeholders()
    {
        var content = Content("user = \"{{q}}\"\n\n[[examples]]\nuser = \"Hi {{q}}\"\nassistant = \"Hello\"\n\n[variables.q]\ntype = \"text\"\nrequired = true\n");

        Assert.Contains("Examples = ([| PromptExample(\"Hi {{q}}\", \"Hello\") |] :> IReadOnlyList<PromptExample>)", content, StringComparison.Ordinal);
        Assert.Contains(
            "Examples = ([| PromptExample(String.Concat([| \"Hi \"; PromptValue.Format(q) |]), \"Hello\") |] :> IReadOnlyList<PromptExample>))",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void TP013_names_that_clash_in_fsharp()
    {
        var result = Emit("test.prompt.toml", "user = \"{{max_tokens}}{{maxTokens}}\"\n[variables.max_tokens]\ntype = \"text\"\n[variables.maxTokens]\ntype = \"text\"\n");

        Assert.Empty(result.Files);
        Assert.Contains("F# property 'MaxTokens'", Assert.Single(result.Problems).Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(3.0, "3.0")]
    [InlineData(0.2, "0.2")]
    [InlineData(1e21, "1E+21")]
    [InlineData(-1.0, "-1.0")]
    public void Floats_always_have_a_decimal_point_or_exponent(double value, string expected) =>
        Assert.Equal(expected, FSharpSyntax.Literal(value));

    private static string Content(string toml) => Assert.Single(Emit("test.prompt.toml", toml).Files).Content;

    private static EmitResult Emit(string file, string toml)
    {
        var parsed = PromptFileParser.Parse("prompts/" + file, toml);
        Assert.True(parsed.Prompt is not null, string.Join("\n", parsed.Problems));
        return Emitter.Emit(parsed.Prompt);
    }
}
