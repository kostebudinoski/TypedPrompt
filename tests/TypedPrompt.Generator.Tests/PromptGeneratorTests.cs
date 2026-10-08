using System.Reflection;
using Microsoft.CodeAnalysis;

namespace TypedPrompt.Generator.Tests;

public class PromptGeneratorTests
{
    private const string Greeting = """
        description = "A greeting"
        system      = "Be {{tone}}."
        user        = "Hello {{name}}!"

        [variables.name]
        type     = "text"
        required = true

        [variables.tone]
        type    = "text"
        default = "friendly"
        """;

    [Fact]
    public void Sample_prompt_files_generate_code_that_compiles()
    {
        var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Prompts"), "*.prompt.toml")
            .Select(path => (path, File.ReadAllText(path)));

        var run = GeneratorHarness.Run(files);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompileErrors);
        Assert.Equal(
            ["AllFieldsPrompt.g.cs", "CodeReviewPrompt.g.cs", "InvoiceExtractPrompt.g.cs", "SentimentClassifyPrompt.g.cs"],
            run.GeneratedFiles.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Generated_class_renders_with_its_values()
    {
        var assembly = GeneratorHarness.Run("Prompts/greeting.prompt.toml", Greeting).Load();

        var prompt = Create(assembly, "Consumer.Prompts.GreetingPrompt", "Anna", "formal");
        var rendered = prompt.Render();

        Assert.Equal("Be formal.", rendered.System);
        Assert.Equal("Hello Anna!", rendered.User);
        Assert.Equal("greeting", prompt.Definition.Key);
        Assert.Equal("A greeting", prompt.Definition.Description);
    }

    [Fact]
    public void Rendered_prompts_carry_the_version_to_log()
    {
        var assembly = GeneratorHarness.Run([("Prompts/greeting.prompt.toml", Greeting), ("Prompts/labelled.prompt.toml", "version = \"2.1\"\n" + Greeting)]).Load();

        var plain = Create(assembly, "Consumer.Prompts.GreetingPrompt", "Anna", Type.Missing);
        var labelled = Create(assembly, "Consumer.Prompts.LabelledPrompt", "Anna", Type.Missing);

        Assert.Matches("^greeting#[0-9a-f]{8}$", plain.Render().Version);
        Assert.Matches("^labelled@2\\.1#[0-9a-f]{8}$", labelled.Render().Version);
        Assert.Equal(plain.Definition.ContentHash, labelled.Definition.ContentHash);
        Assert.Equal(64, plain.Definition.ContentHash.Length);
        Assert.Equal("2.1", labelled.Definition.VersionLabel);
    }

    [Fact]
    public void Generated_code_names_the_generator_version_without_the_commit()
    {
        var run = GeneratorHarness.Run("Prompts/greeting.prompt.toml", Greeting);
        var code = run.Compilation.SyntaxTrees.Single(t => t.FilePath.EndsWith("GreetingPrompt.g.cs", StringComparison.Ordinal)).ToString();

        Assert.Contains($"GeneratedCode(\"TypedPrompt\", \"{PromptGenerator.Version}\")", code, StringComparison.Ordinal);
        Assert.Matches("^[0-9]+\\.[0-9]+\\.[0-9]+(-[0-9A-Za-z.]+)?$", PromptGenerator.Version);
    }

    [Fact]
    public void Optional_values_use_their_defaults()
    {
        var assembly = GeneratorHarness.Run("Prompts/greeting.prompt.toml", Greeting).Load();

        var prompt = Create(assembly, "Consumer.Prompts.GreetingPrompt", "Anna", Type.Missing);

        Assert.Equal("Be friendly.", prompt.Render().System);
    }

    [Fact]
    public void Numbers_and_booleans_render_the_same_on_every_machine()
    {
        const string toml = """
            user = "{{count}} items, urgent: {{urgent}}, missing: [{{note}}]"

            [variables.count]
            type     = "number"
            required = true

            [variables.urgent]
            type    = "boolean"
            default = false

            [variables.note]
            type = "text"
            """;
        var assembly = GeneratorHarness.Run("Prompts/order.prompt.toml", toml).Load();

        var prompt = Create(assembly, "Consumer.Prompts.OrderPrompt", 2.5, true, Type.Missing);

        Assert.Equal("2.5 items, urgent: true, missing: []", prompt.Render().User);
    }

    [Fact]
    public void Examples_render_between_system_and_user()
    {
        const string toml = """
            system = "Classify."
            user   = "{{text}}"

            [[examples]]
            user      = "I love it"
            assistant = "positive"

            [[examples]]
            user      = "It broke after {{days}} days"
            assistant = "negative"

            [variables.text]
            type     = "text"
            required = true

            [variables.days]
            type    = "number"
            default = 2
            """;
        var assembly = GeneratorHarness.Run("Prompts/sentiment.prompt.toml", toml).Load();

        var rendered = Create(assembly, "Consumer.Prompts.SentimentPrompt", "Meh", Type.Missing).Render();

        Assert.Equal(
            [
                (PromptRole.System, "Classify."),
                (PromptRole.User, "I love it"),
                (PromptRole.Assistant, "positive"),
                (PromptRole.User, "It broke after 2 days"),
                (PromptRole.Assistant, "negative"),
                (PromptRole.User, "Meh"),
            ],
            rendered.Messages.Select(m => (m.Role, m.Text)));
    }

    [Fact]
    public void Leaving_out_a_required_value_does_not_compile()
    {
        var run = GeneratorHarness.Run("Prompts/greeting.prompt.toml", Greeting, "class Use { object M() => new Consumer.Prompts.GreetingPrompt(); }");

        Assert.Contains(run.CompileErrors, d => d.Id == "CS7036");
    }

    [Fact]
    public void Problems_become_diagnostics_at_the_file_and_line()
    {
        var run = GeneratorHarness.Run("Prompts/broken.prompt.toml", "user = '''\nHello\n{{nmae}}\n'''\n\n[variables.name]\ntype = \"text\"\n");

        var diagnostic = Assert.Single(run.GeneratorDiagnostics, d => d.Id == "TP004");
        var span = diagnostic.Location.GetLineSpan();
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Prompts/broken.prompt.toml", span.Path);
        Assert.Equal(2, span.StartLinePosition.Line);
        Assert.Contains("Did you mean 'name'?", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Empty(run.GeneratedFiles);
    }

    [Fact]
    public void Warnings_do_not_stop_generation()
    {
        var run = GeneratorHarness.Run("Prompts/greeting.prompt.toml", Greeting + "\n[variables.unused]\ntype = \"text\"\n");

        Assert.Equal(DiagnosticSeverity.Warning, Assert.Single(run.GeneratorDiagnostics).Severity);
        Assert.Single(run.GeneratedFiles);
    }

    [Fact]
    public void A_broken_file_does_not_stop_the_others()
    {
        var run = GeneratorHarness.Run([("Prompts/greeting.prompt.toml", Greeting), ("Prompts/broken.prompt.toml", "user = \"never closed\n")]);

        Assert.Contains(run.GeneratorDiagnostics, d => d.Id == "TP001");
        Assert.Equal(["GreetingPrompt.g.cs"], run.GeneratedFiles);
    }

    [Fact]
    public void Files_that_clash_generate_nothing()
    {
        var run = GeneratorHarness.Run([("a/support-reply.prompt.toml", Greeting), ("b/support_reply.prompt.toml", Greeting), ("greeting.prompt.toml", Greeting)]);

        Assert.Equal(2, run.GeneratorDiagnostics.Count(d => d.Id == "TP008"));
        Assert.Equal(["GreetingPrompt.g.cs"], run.GeneratedFiles);
    }

    [Fact]
    public void Files_whose_class_names_differ_only_in_case_clash_instead_of_crashing_the_generator()
    {
        var run = GeneratorHarness.Run(
            [("Prompts/fooBar.prompt.toml", Greeting), ("Prompts/old/foobar.prompt.toml", Greeting), ("Prompts/greeting.prompt.toml", Greeting)]);

        Assert.DoesNotContain(run.GeneratorDiagnostics, d => d.Id == "CS8785");
        Assert.Equal(2, run.GeneratorDiagnostics.Count(d => d.Id == "TP008"));
        Assert.Equal(["GreetingPrompt.g.cs"], run.GeneratedFiles);
    }

    [Fact]
    public void Other_additional_files_are_ignored()
    {
        var run = GeneratorHarness.Run([("appsettings.toml", "x = 1"), ("notes.txt", "hello")]);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.GeneratedFiles);
    }

    [Theory]
    [InlineData(null, "MyApp", "MyApp.Prompts.GreetingPrompt")]
    [InlineData("Company.AI", "MyApp", "Company.AI.GreetingPrompt")]
    [InlineData(null, null, "Prompts.GreetingPrompt")]
    public void Namespace_comes_from_the_msbuild_properties(string? configured, string? root, string expected)
    {
        var properties = new Dictionary<string, string>();
        if (configured is not null)
        {
            properties["build_property.TypedPromptNamespace"] = configured;
        }

        if (root is not null)
        {
            properties["build_property.RootNamespace"] = root;
        }

        var assembly = GeneratorHarness.Run([("greeting.prompt.toml", Greeting)], properties: properties).Load();

        Assert.NotNull(assembly.GetType(expected));
    }

    private static ITypedPrompt Create(Assembly assembly, string type, params object?[] arguments) =>
        (ITypedPrompt)Activator.CreateInstance(
            assembly.GetType(type, throwOnError: true)!,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.OptionalParamBinding,
            binder: null,
            arguments,
            culture: null)!;
}
