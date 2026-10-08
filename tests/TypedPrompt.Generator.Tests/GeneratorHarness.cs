using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace TypedPrompt.Generator.Tests;

/// <summary>Runs the real generator on in-memory prompt files and compiles the result, like a build would.</summary>
internal static class GeneratorHarness
{
    private static readonly ImmutableArray<MetadataReference> References =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => MetadataReference.CreateFromFile(path)),
        MetadataReference.CreateFromFile(typeof(ITypedPrompt).Assembly.Location),
    ];

    public static GeneratorRun Run(IEnumerable<(string Path, string Text)> files, string code = "", Dictionary<string, string>? properties = null)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create(
            "Consumer",
            [CSharpSyntaxTree.ParseText(code, parseOptions)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(
            [new PromptGenerator().AsSourceGenerator()],
            [.. files.Select(f => (AdditionalText)new InMemoryText(f.Path, f.Text))],
            parseOptions,
            new Options(properties ?? new Dictionary<string, string> { ["build_property.RootNamespace"] = "Consumer" }));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics, TestContext.Current.CancellationToken);
        return new GeneratorRun(output, generatorDiagnostics);
    }

    public static GeneratorRun Run(string path, string text, string code = "") => Run([(path, text)], code);

    internal sealed record GeneratorRun(Compilation Compilation, ImmutableArray<Diagnostic> GeneratorDiagnostics)
    {
        public IReadOnlyList<string> GeneratedFiles => Compilation.SyntaxTrees.Select(t => Path.GetFileName(t.FilePath)).Where(n => n.EndsWith(".g.cs", StringComparison.Ordinal)).ToList();

        public IReadOnlyList<Diagnostic> CompileErrors =>
            Compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        /// <summary>Compiles to memory and loads the assembly, so generated classes can be created and called.</summary>
        public Assembly Load()
        {
            using var stream = new MemoryStream();
            var emitted = Compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
            stream.Position = 0;
            return new AssemblyLoadContext(null, isCollectible: true).LoadFromStream(stream);
        }
    }

    private sealed class InMemoryText(string path, string text) : AdditionalText
    {
        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
    }

    private sealed class Options(Dictionary<string, string> properties) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Values(properties);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new Values([]);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new Values([]);

        private sealed class Values(Dictionary<string, string> values) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value) =>
                values.TryGetValue(key, out value);
        }
    }
}
