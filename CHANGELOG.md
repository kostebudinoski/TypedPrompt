# Changelog

All notable changes are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and
the package follows [Semantic Versioning](https://semver.org/). While the version is `0.x`, minor versions may contain
breaking changes. Versions come from git tags (`v0.1.0-preview.1` → `0.1.0-preview.1`).

## [Unreleased]

## [0.1.0-preview.1] - 2026-10-08

First preview.

### Added
- Source generator: every `*.prompt.toml` file becomes a strongly typed C# class at build time, named after the file
  (`ticket-summarize.prompt.toml` → `TicketSummarizePrompt`) in `$(RootNamespace).Prompts` (or `TypedPromptNamespace`).
- Prompt format: `system`, `user`, `{{placeholders}}` (`\{{` for a literal `{{`), typed `[variables.*]` (`text`,
  `number`, `boolean`; required or with a default), `[[examples]]` for few-shot, `[model]` (name, temperature, top_p,
  top_k, max_output_tokens, stop_sequences, effort), `[output]` JSON schema, `[metadata]`, `tags`, `description`, `key`,
  and an optional `version` label.
- Generated classes: values in the constructor (required variables are required parameters), a property per value,
  a static `Definition`, `Render()`, and `ITypedPrompt` for generic code.
- `RenderedPrompt`: `System`, `User`, `Examples`, `Messages` (ready for any chat API), `Text`, `Model`, `OutputSchema`,
  and `Version` (`key@label#hash8`) to log with every answer.
- Content hash computed at build time from everything that reaches the model, identical in Visual Studio and
  `dotnet build`.
- Build errors at the file and line for mistakes in prompt files (TP001–TP013), with "did you mean" hints.
- One NuGet package: runtime for netstandard2.0, net8.0 and net10.0 with no dependencies; the generator and its TOML
  parser are build-time only and never reach the app's output. `Prompts/**/*.prompt.toml` is picked up automatically
  (`TypedPromptAutoInclude=false` to opt out).

[Unreleased]: https://github.com/kostebudinoski/TypedPrompt/compare/v0.1.0-preview.1...HEAD
[0.1.0-preview.1]: https://github.com/kostebudinoski/TypedPrompt/releases/tag/v0.1.0-preview.1
