namespace TypedPrompt.Tests;

public class PromptDefinitionTests
{
    [Fact]
    public void Only_key_and_user_template_are_needed()
    {
        var definition = new PromptDefinition("greeting", "Hello {{name}}!");

        Assert.Equal(("greeting", "Hello {{name}}!"), (definition.Key, definition.UserTemplate));
        Assert.Null(definition.SystemTemplate);
        Assert.Null(definition.Description);
        Assert.Null(definition.Model);
        Assert.Null(definition.OutputSchema);
        Assert.Empty(definition.Tags);
        Assert.Empty(definition.Metadata);
        Assert.Empty(definition.Variables);
    }

    [Fact]
    public void Everything_else_is_set_with_initializers()
    {
        var definition = new PromptDefinition("ticket-summarize", "{{ticket}}")
        {
            SystemTemplate = "Answer in {{language}}.",
            Tags = ["support"],
            Metadata = new Dictionary<string, string> { ["owner"] = "support-team" },
            Variables = [new VariableInfo("ticket", VariableType.Text, Required: true)],
        };

        Assert.Equal("Answer in {{language}}.", definition.SystemTemplate);
        Assert.Equal("support-team", definition.Metadata["owner"]);
        Assert.True(Assert.Single(definition.Variables).Required);
    }

    [Theory]
    [InlineData("", null, "greeting")]
    [InlineData("91fffb16ea949e1465d97586b8aedc7edb855709c60a07727fa36c8446fffece", null, "greeting#91fffb16")]
    [InlineData("91fffb16ea949e1465d97586b8aedc7edb855709c60a07727fa36c8446fffece", "2.1", "greeting@2.1#91fffb16")]
    public void Version_is_key_label_and_short_hash(string hash, string? label, string expected)
    {
        var definition = new PromptDefinition("greeting", "Hi") { ContentHash = hash, VersionLabel = label };

        Assert.Equal(expected, definition.Version);
    }

    [Fact]
    public void Rendered_version_defaults_to_the_key() =>
        Assert.Equal("greeting", new RenderedPrompt("greeting", null, "Hi").Version);

    [Fact]
    public void A_prompt_renders_through_the_interface_without_knowing_its_variables()
    {
        ITypedPrompt prompt = new GreetingPrompt("Anna");

        var rendered = prompt.Render();

        Assert.Equal("Hello Anna!", rendered.User);
        Assert.Equal("greeting", prompt.Definition.Key);
    }

    /// <summary>The shape the generator produces, reduced to one variable.</summary>
    private sealed class GreetingPrompt(string name) : ITypedPrompt
    {
        public static PromptDefinition Definition { get; } = new("greeting", "Hello {{name}}!");

        PromptDefinition ITypedPrompt.Definition => Definition;

        public RenderedPrompt Render() => new(Definition.Key, null, string.Concat("Hello ", PromptValue.Format(name), "!"));
    }
}
