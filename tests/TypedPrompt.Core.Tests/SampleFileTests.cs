namespace TypedPrompt.Core.Tests;

/// <summary>The sample prompt files are read completely and without problems.</summary>
public class SampleFileTests
{
    [Fact]
    public void All_fields_file_reads_every_field()
    {
        var prompt = Read("all-fields.prompt.toml");

        Assert.Equal(("support.reply", "AllFields"), (prompt.Key, prompt.Name));
        Assert.Equal("Draft a reply to a customer", prompt.Description);
        Assert.Equal(["support", "email"], prompt.Tags);
        Assert.Equal([new("owner", "support-team"), new KeyValuePair<string, string>("ticket", "SUP-142")], prompt.Metadata);

        Assert.StartsWith("You are a support agent for {{company}}.", prompt.System?.Text, StringComparison.Ordinal);
        Assert.Equal("Customer message:\n{{message}}\n\nOffer a refund: {{offer_refund}}", prompt.User.Text);

        Assert.Equal(["message", "company", "tone", "agent_name", "max_words", "offer_refund"], prompt.Variables.Select(v => v.Name));
        var variables = prompt.Variables.ToDictionary(v => v.Name);
        Assert.Equal((VariableKind.Text, true, null, "The customer's message"), Describe(variables["message"]));
        Assert.Equal((VariableKind.Text, false, "Acme", null), Describe(variables["company"]));
        Assert.Equal((VariableKind.Text, false, "friendly", "e.g. friendly, formal, apologetic"), Describe(variables["tone"]));
        Assert.Equal((VariableKind.Text, false, null, null), Describe(variables["agent_name"]));
        Assert.Equal((VariableKind.Number, false, 150.0, null), Describe(variables["max_words"]));
        Assert.Equal((VariableKind.Boolean, false, false, null), Describe(variables["offer_refund"]));

        var model = prompt.Model!;
        Assert.Equal(("claude-sonnet-5", 0.4, 0.9, 40, 600), (model.Name, model.Temperature, model.TopP, model.TopK, model.MaxOutputTokens));
        Assert.Equal(["\n\nCustomer:"], model.StopSequences);
        Assert.Equal("medium", model.Effort);

        Assert.StartsWith("{", prompt.OutputSchema, StringComparison.Ordinal);
        Assert.EndsWith("}", prompt.OutputSchema, StringComparison.Ordinal);
        Assert.Contains("\"refund\"", prompt.OutputSchema, StringComparison.Ordinal);
    }

    [Fact]
    public void All_fields_file_reads_its_examples_in_order()
    {
        var examples = Read("all-fields.prompt.toml").Examples;

        Assert.Equal(2, examples.Count);
        Assert.Equal("Customer message:\nWhere is my parcel?", examples[0].User.Text);
        Assert.Contains(examples[0].Assistant.Segments, s => s is { Kind: TemplateSegmentKind.Placeholder, Value: "company" });
        Assert.StartsWith("{\"subject\": \"Sorry about the lamp\"", examples[1].Assistant.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Escaped_braces_become_literal_text()
    {
        var system = Read("all-fields.prompt.toml").System!;

        Assert.Contains(system.Segments, s => s.Kind == TemplateSegmentKind.Text && s.Value.Contains("Write {{placeholders}} literally", StringComparison.Ordinal));
        Assert.DoesNotContain(system.Segments, s => s is { Kind: TemplateSegmentKind.Placeholder, Value: "placeholders" });
    }

    [Fact]
    public void Templates_are_split_into_text_and_placeholders()
    {
        var user = Read("all-fields.prompt.toml").User;

        Assert.Equal(
            [
                (TemplateSegmentKind.Text, "Customer message:\n"),
                (TemplateSegmentKind.Placeholder, "message"),
                (TemplateSegmentKind.Text, "\n\nOffer a refund: "),
                (TemplateSegmentKind.Placeholder, "offer_refund"),
            ],
            user.Segments.Select(s => (s.Kind, s.Value)));
    }

    [Theory]
    [InlineData("invoice-extract.prompt.toml", "invoice-extract", "InvoiceExtract", 2)]
    [InlineData("sentiment-classify.prompt.toml", "sentiment-classify", "SentimentClassify", 2)]
    [InlineData("code-review.prompt.toml", "code-review", "CodeReview", 5)]
    public void Other_sample_files_are_valid(string file, string key, string name, int variables)
    {
        var prompt = Read(file);

        Assert.Equal((key, name, variables), (prompt.Key, prompt.Name, prompt.Variables.Count));
    }

    [Fact]
    public void Windows_line_endings_give_the_same_prompt()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Prompts", "all-fields.prompt.toml");
        var unix = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
        var windows = unix.Replace("\n", "\r\n", StringComparison.Ordinal);

        var a = PromptFileParser.Parse(path, unix).Prompt!;
        var b = PromptFileParser.Parse(path, windows).Prompt!;

        Assert.Equal(a.System?.Text, b.System?.Text);
        Assert.Equal(a.User.Text, b.User.Text);
        Assert.Equal(a.OutputSchema, b.OutputSchema);
    }

    private static ParsedPrompt Read(string file)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Prompts", file);
        var result = PromptFileParser.Parse(path, File.ReadAllText(path));

        Assert.True(result.Problems.Count == 0, string.Join("\n", result.Problems));
        return result.Prompt!;
    }

    private static (VariableKind, bool, object?, string?) Describe(ParsedVariable variable) =>
        (variable.Kind, variable.Required, variable.Default, variable.Description);
}
