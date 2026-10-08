namespace TypedPrompt.Core.Tests;

public class PromptHasherTests
{
    private const string Greeting = """
        user = "Hello {{name}}!"

        [variables.name]
        type     = "text"
        required = true
        """;

    private const string Full = """
        key         = "support.reply"
        version     = "2.1"
        description = "Draft a reply"
        tags        = ["support"]
        system      = "Be {{tone}}."
        user        = "{{message}}"

        [[examples]]
        user      = "Hi"
        assistant = "Hello!"

        [variables.message]
        type        = "text"
        required    = true
        description = "The message"

        [variables.tone]
        type    = "text"
        default = "friendly"

        [model]
        name        = "claude-sonnet-5"
        temperature = 0.2
        effort      = "high"

        [output]
        schema = '''{ "type": "object" }'''

        [metadata]
        owner = "support-team"
        """;

    [Fact]
    public void Canonical_text_is_exact() =>
        Assert.Equal(
            "typedprompt 1\nsystem null\nuser \"Hello {{name}}!\"\nvariable \"name\" text null\noutput null\n",
            PromptHasher.CanonicalText(Parse(Greeting)));

    [Fact]
    public void Hash_is_sha256_of_the_canonical_text() =>
        // Golden value, computed independently with Python's hashlib over the canonical text above.
        Assert.Equal("91fffb16ea949e1465d97586b8aedc7edb855709c60a07727fa36c8446fffece", Parse(Greeting).ContentHash);

    [Theory]
    [InlineData("key         = \"support.reply\"", "key         = \"other.key\"")]
    [InlineData("version     = \"2.1\"", "version     = \"3.0\"")]
    [InlineData("description = \"Draft a reply\"", "description = \"Something else\"")]
    [InlineData("tags        = [\"support\"]", "tags        = [\"email\"]")]
    [InlineData("description = \"The message\"", "description = \"Changed docs\"")]
    [InlineData("required    = true\ndescription", "description")]
    [InlineData("owner = \"support-team\"", "owner = \"other-team\"")]
    [InlineData("schema = '''{ \"type\": \"object\" }'''", "schema = '''\n{ \"type\": \"object\" }\n'''")]
    public void Changes_that_do_not_reach_the_model_keep_the_hash(string original, string replacement)
    {
        var changed = Parse(Full.Replace(original, replacement, StringComparison.Ordinal));

        Assert.Equal(Parse(Full).ContentHash, changed.ContentHash);
    }

    [Theory]
    [InlineData("system      = \"Be {{tone}}.\"", "system      = \"Be very {{tone}}.\"")]
    [InlineData("user        = \"{{message}}\"", "user        = \"Reply to: {{message}}\"")]
    [InlineData("assistant = \"Hello!\"", "assistant = \"Hello there!\"")]
    [InlineData("default = \"friendly\"", "default = \"formal\"")]
    [InlineData("temperature = 0.2", "temperature = 0.3")]
    [InlineData("effort      = \"high\"", "effort      = \"low\"")]
    [InlineData("name        = \"claude-sonnet-5\"", "name        = \"claude-opus-5\"")]
    [InlineData("schema = '''{ \"type\": \"object\" }'''", "schema = '''{ \"type\": \"array\" }'''")]
    public void Every_change_the_model_would_notice_changes_the_hash(string original, string replacement)
    {
        var changed = Parse(Full.Replace(original, replacement, StringComparison.Ordinal));

        Assert.NotEqual(Parse(Full).ContentHash, changed.ContentHash);
    }

    [Fact]
    public void Variable_order_and_line_endings_do_not_change_the_hash()
    {
        var reordered = Full.Replace("[variables.message]\ntype        = \"text\"\nrequired    = true\ndescription = \"The message\"\n\n[variables.tone]\ntype    = \"text\"\ndefault = \"friendly\"",
            "[variables.tone]\ntype    = \"text\"\ndefault = \"friendly\"\n\n[variables.message]\ntype        = \"text\"\nrequired    = true\ndescription = \"The message\"", StringComparison.Ordinal);
        var windows = Full.Replace("\n", "\r\n", StringComparison.Ordinal);

        Assert.NotEqual(Full, reordered);
        Assert.Equal(Parse(Full).ContentHash, Parse(reordered).ContentHash);
        Assert.Equal(Parse(Full).ContentHash, Parse(windows).ContentHash);
    }

    [Fact]
    public void Version_combines_key_label_and_short_hash()
    {
        var withLabel = Parse(Full);
        var withoutLabel = Parse(Greeting);

        Assert.Equal($"support.reply@2.1#{withLabel.ContentHash[..8]}", withLabel.Version);
        Assert.Equal("test#91fffb16", withoutLabel.Version);
    }

    [Theory]
    [InlineData("version = \"\"\n")]
    [InlineData("version = \"two point one\"\n")]
    [InlineData("version = 2\n")]
    public void TP011_invalid_version_labels(string version)
    {
        var result = PromptFileParser.Parse("prompts/test.prompt.toml", version + Greeting);

        Assert.Equal("TP011", Assert.Single(result.Problems).Id);
    }

    private static ParsedPrompt Parse(string toml)
    {
        var result = PromptFileParser.Parse("prompts/test.prompt.toml", toml);
        Assert.True(result.Prompt is not null, string.Join("\n", result.Problems));
        return result.Prompt;
    }
}
