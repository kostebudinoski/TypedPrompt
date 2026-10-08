using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using TypedPrompt.Core;

namespace TypedPrompt.Generator;

/// <summary>
/// Turns every <c>*.prompt.toml</c> additional file into a strongly typed C# prompt class. Problems in the files become
/// compiler diagnostics (TP001…) at the file and line; a file with errors generates no class.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class PromptGenerator : IIncrementalGenerator
{
    /// <summary>MSBuild property that sets the namespace of the generated classes.</summary>
    public const string NamespaceProperty = "build_property.TypedPromptNamespace";

    private const string RootNamespaceProperty = "build_property.RootNamespace";

    /// <summary>This generator's version (e.g. <c>0.1.0</c>), written into <c>[GeneratedCode]</c> on every generated class.</summary>
    internal static readonly string Version = ReadVersion();

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var files = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".prompt.toml", StringComparison.OrdinalIgnoreCase))
            .Select((file, cancellationToken) => new PromptFile(file.Path, file.GetText(cancellationToken)?.ToString() ?? string.Empty));

        var targetNamespace = context.AnalyzerConfigOptionsProvider.Select((options, _) => Namespace(options.GlobalOptions));

        context.RegisterSourceOutput(files.Collect().Combine(targetNamespace), static (output, input) => Generate(output, input.Left, input.Right));
    }

    private static void Generate(SourceProductionContext output, ImmutableArray<PromptFile> files, string targetNamespace)
    {
        var parsed = new List<ParsedPrompt>();
        foreach (var file in files.OrderBy(f => f.Path, StringComparer.Ordinal))
        {
            output.CancellationToken.ThrowIfCancellationRequested();
            var result = PromptFileParser.Parse(file.Path, file.Text);
            Report(output, result.Problems);
            if (result.Prompt is { } prompt)
            {
                parsed.Add(prompt);
            }
        }

        var duplicates = PromptSetValidator.Validate(parsed);
        Report(output, duplicates);
        var clashing = new HashSet<string>(duplicates.Select(p => p.Path), StringComparer.Ordinal);

        var emitter = new CSharpEmitter(new CSharpEmitOptions(targetNamespace, GeneratorVersion: Version));
        foreach (var prompt in parsed.Where(p => !clashing.Contains(p.Path)))
        {
            var emitted = emitter.Emit(prompt);
            Report(output, emitted.Problems);
            foreach (var generated in emitted.Files)
            {
                output.AddSource(generated.Name, SourceText.From(generated.Content, System.Text.Encoding.UTF8));
            }
        }
    }

    /// <summary>The informational version without the commit part: <c>0.1.0-preview.1+3f2a9c1</c> → <c>0.1.0-preview.1</c>.</summary>
    private static string ReadVersion()
    {
        var informational = typeof(PromptGenerator).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), inherit: false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion;
        var version = informational ?? typeof(PromptGenerator).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        var plus = version.IndexOf('+');
        return plus < 0 ? version : version.Substring(0, plus);
    }

    /// <summary>The <c>TypedPromptNamespace</c> property, else <c>$(RootNamespace).Prompts</c>, else <c>Prompts</c>.</summary>
    private static string Namespace(AnalyzerConfigOptions options)
    {
        if (options.TryGetValue(NamespaceProperty, out var configured) && !string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        return options.TryGetValue(RootNamespaceProperty, out var root) && !string.IsNullOrWhiteSpace(root)
            ? root.Trim() + ".Prompts"
            : "Prompts";
    }

    private static void Report(SourceProductionContext output, IEnumerable<PromptProblem> problems)
    {
        foreach (var problem in problems)
        {
            output.ReportDiagnostic(Diagnostics.Create(problem));
        }
    }

    /// <summary>A prompt file's path and text: an equatable value, so the generator only reruns when a file changes.</summary>
    private sealed record PromptFile(string Path, string Text);
}
