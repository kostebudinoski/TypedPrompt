# TypedPrompt

Write your LLM prompts as `.prompt.toml` files. TypedPrompt turns each one into a **strongly typed C# class at build time**:
one constructor parameter per variable, a `Render()` method, and the model settings, output schema and metadata as
properties. A missing value or a typo in a placeholder is a compile error, not a surprise in production.

```csharp
var prompt = new TicketSummarizePrompt(ticket: "Printer is on fire", language: "German");
RenderedPrompt rendered = prompt.Render();   // messages + model settings + JSON schema, ready for any LLM SDK
```

No runtime parsing, no reflection, no LLM SDK dependency.

## Install

```
dotnet add package TypedPrompt
```

Works in any project on .NET 8 or later (and .NET Standard 2.0 libraries) with C# 9 or later.

## 1. Write a prompt

Put files in a `Prompts` folder in your project. They are picked up automatically:

```toml
# Prompts/ticket-summarize.prompt.toml
description = "Summarise a support ticket"

system = """
You are a support assistant. Answer in {{language}}.
"""
user = "{{ticket}}"

[variables.ticket]
type        = "text"
required    = true
description = "The ticket text"

[variables.language]
type    = "text"
default = "English"

[model]
name              = "claude-sonnet-5"
temperature       = 0.2
max_output_tokens = 800
```

## 2. Use the generated class

Build the project. The file name becomes the class name with a `Prompt` suffix (`ticket-summarize` → `TicketSummarizePrompt`),
in the namespace `<YourRootNamespace>.Prompts`:

```csharp
using MyApp.Prompts;
using TypedPrompt;

var prompt = new TicketSummarizePrompt(ticket: "Login page is slow since Monday.");   // language defaults to "English"
var rendered = prompt.Render();

rendered.System;        // "You are a support assistant. Answer in English."
rendered.User;          // "Login page is slow since Monday."
rendered.Messages;      // [system, (examples), user], ready to map to your SDK's message type
rendered.Model;         // claude-sonnet-5, temperature 0.2, max output 800 (and Effort, when set)
rendered.OutputSchema;  // the JSON schema, or null for a plain-text answer
```

Pass the result to whatever client you use. For example with `Microsoft.Extensions.AI`:

```csharp
var messages = rendered.Messages.Select(m => new ChatMessage(
    m.Role switch { PromptRole.System => ChatRole.System, PromptRole.Assistant => ChatRole.Assistant, _ => ChatRole.User },
    m.Text));
var options = new ChatOptions { ModelId = rendered.Model?.Name, Temperature = (float?)rendered.Model?.Temperature };
var response = await chatClient.GetResponseAsync(messages, options);
```

### Without knowing the prompt

Every generated class implements `ITypedPrompt`, so you can render, log or test prompts generically:

```csharp
void Log(ITypedPrompt prompt) => logger.LogInformation("{Key}: {Text}", prompt.Definition.Key, prompt.Render().Text);
```

The definition (key, templates, variables, model, schema, metadata) is also available without any values:
`TicketSummarizePrompt.Definition`.

## F#

F# projects can use the generated C# classes today: put the prompt files in a small C# class library and reference it.
F# calls them with named and optional arguments (`TicketSummarizePrompt(ticket = "...", language = "German")`), fully
checked at compile time.

Native F# types are prepared but not shipped yet: the F# compiler does not run source generators, so they will come with
a TypedPrompt command-line tool.

## File format

| Field | Required | Meaning |
|---|---|---|
| `user` | yes | The request text. |
| `system` | no | Instructions for the model. |
| `[[examples]]` | no | Few-shot examples: one section per example, each with `user` and `assistant`. |
| `key` | no | Stable id; defaults to the file name without `.prompt.toml`. |
| `version` | no | A human label such as `"2.1"`; see [Versions](#versions). |
| `description` | no | Becomes the class summary. |
| `tags` | no | `["support", "email"]` |
| `[variables.<name>]` | for each placeholder | `type` (`text`, `number`, `boolean`), `required`, `default`, `description`. |
| `[model]` | no | `name`, `temperature`, `top_p`, `top_k`, `max_output_tokens`, `stop_sequences`, `effort` (`minimal`, `low`, `medium`, `high`, `max`). All optional. |
| `[output]` | no | `schema = '''{ ... }'''`: the JSON schema the answer must follow. |
| `[metadata]` | no | Your own text values, e.g. `owner = "support-team"`. |

**Examples.** Each `[[examples]]` section is one exchange that shows the model what a good answer looks like. They are
sent between the system text and the user text, in file order, as user/assistant message pairs:

```toml
system = "Classify the sentiment."
user   = "{{text}}"

[[examples]]
user      = "I love it"
assistant = "positive"

[[examples]]
user      = "It broke after two days"
assistant = "negative"
```

Messages: `system → user → assistant → user → assistant → user`. Placeholders work in examples too.

**Order in the file.** In TOML, every `key = value` below a `[section]` or `[[examples]]` header belongs to that
section. So write the top-level fields (`key`, `description`, `tags`, `system`, `user`) first, then the sections.
A `user` written below `[[examples]]` becomes part of the last example; the build error says so.

**Placeholders.** `{{name}}` inserts a variable; spaces inside the braces are fine. `\{{` writes a literal `{{`: put such
text in a `'''...'''` string, because inside `"..."` TOML itself treats the backslash as an escape.
There are no loops or conditions: keep logic in your code.

**Variables become parameters:**

| In the file | C# parameter |
|---|---|
| `type = "text"`, `required = true` | `string ticket` (required) |
| `type = "text"`, `default = "English"` | `string language = "English"` |
| `type = "number"`, `default = 3` | `double maxSentences = 3` |
| `type = "boolean"`, `default = false` | `bool offerRefund = false` |
| no `required`, no `default` | `string? agentName = null` (renders as empty text) |

Names are converted to C# style (`max_sentences` → parameter `maxSentences`, property `MaxSentences`). Required
parameters come first. Numbers render the same on every machine (`0.2`, never `0,2`).

See [`samples/Sample/Prompts`](samples/Sample/Prompts) for realistic prompts: invoice extraction with a JSON schema,
classification with few-shot examples, and a code review with typed options.

## Versions

Every prompt gets a **content hash at build time**: SHA-256 of everything that changes what the model receives or how it
is called (system, examples, user, variables and their defaults, model settings, output schema). Change one word and it
changes; change nothing and it stays the same. Key, description, tags, metadata and variable descriptions don't affect
it, so fixing documentation doesn't make a new version.

```csharp
var rendered = new TicketSummarizePrompt(ticket: "...").Render();
logger.LogInformation("Prompt {Version} answered: {Answer}", rendered.Version, answer);
// ticket-summarize#6cb228b5

TicketSummarizePrompt.Definition.ContentHash;   // full SHA-256
```

Optionally give the prompt a human label; it is shown next to the hash, which stays authoritative (a forgotten bump
cannot hide a change):

```toml
version = "2.1"          # → rendered.Version == "ticket-summarize@2.1#6cb228b5"
```

## Errors at build time

Mistakes in a prompt file are reported by the compiler, at the file and line, like any other error:

```
Prompts/ticket-summarize.prompt.toml(9,1): error TP004: {{tikcet}} is not declared in [variables]. Did you mean 'ticket'?
```

| Id | Severity | When |
|---|---|---|
| TP001 | error | Not valid TOML |
| TP002 | error | Unknown field (with a "did you mean") |
| TP003 | error | `user` missing |
| TP004 | error | Placeholder not declared in `[variables]` |
| TP005 | warning | Variable declared but not used |
| TP006 | error | Invalid variable name or type, or a default of the wrong type |
| TP007 | warning | Variable is required and also has a default |
| TP008 | error | Two files give the same class name or key |
| TP009 | error | Unclosed `{{` or invalid placeholder |
| TP010 | error | `[output]` schema is not valid JSON |
| TP011 | error | Wrong value or type (e.g. `temperature = "hot"`, `top_p = 1.5`, `effort = "hgih"`) |
| TP012 | error | File name cannot become a class name |
| TP013 | error | Variable names clash in C# (`max_tokens` and `maxTokens`, or `render`) |

A file with errors generates no class; the other files still do.

## Settings

| MSBuild property | Default | Effect |
|---|---|---|
| `TypedPromptNamespace` | `$(RootNamespace).Prompts` | Namespace of the generated classes. |
| `TypedPromptAutoInclude` | `true` | Set to `false` to add files yourself with `<AdditionalFiles Include="..." />`. |

```xml
<PropertyGroup>
  <TypedPromptNamespace>MyCompany.Ai.Prompts</TypedPromptNamespace>
</PropertyGroup>
```

To see the generated code, set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>`; the files appear under
`obj/.../generated/TypedPrompt.CSharp.Generator/`. Generated classes are `partial`, so you can add members in your own file.

## Build from source

```
dotnet build
dotnet test
dotnet run --project samples/Sample
dotnet pack src/TypedPrompt -c Release -o artifacts     # one package: runtime + generator
```

## Releasing

Versions come from git tags ([MinVer](https://github.com/adamralph/minver)): untagged builds are `0.1.0-preview.0.N`.
To publish, update `CHANGELOG.md`, then tag and push:

```
git tag v0.1.0-preview.1
git push origin v0.1.0-preview.1
```

The `Release` workflow builds, tests, packs exactly that version and pushes it to NuGet.org. It needs a repository secret
`NUGET_API_KEY` (a NuGet.org API key with push rights for `TypedPrompt`).
