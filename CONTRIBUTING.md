# Contributing

Thanks for helping. Bugs and ideas go in [issues](https://github.com/kostebudinoski/TypedPrompt/issues/new/choose);
for anything larger than a small fix, open an issue first so we can agree on the approach.

## Build and test

You need the .NET SDK from `global.json` (10.0) and the .NET 8 runtime for the `net8.0` tests.

```
dotnet build
dotnet test
dotnet run --project samples/Sample
```

Warnings are errors, so a clean build is part of the check. CI runs the same on Windows and Linux.

## Where things live

| Project | What it is |
|---|---|
| `src/TypedPrompt.Core` | Language-neutral: TOML parsing, validation, problems (TP001…), hashing, the C# and F# emitters |
| `src/TypedPrompt.CSharp.Generator` | The Roslyn source generator; turns Core's problems into compiler diagnostics |
| `src/TypedPrompt` | The runtime the generated code calls, and the NuGet package |
| `tests/*` | One test project per source project |

Most changes belong in Core and are tested there (`tests/TypedPrompt.Core.Tests`).

## Rules that are easy to miss

- **The runtime's public API is a binary contract.** Prompt libraries are compiled against one version and may run
  against a newer one. Add members; don't change or remove them. The build enforces this: a new public member fails
  with RS0016 until it is listed in `src/TypedPrompt/PublicAPI.Unshipped.txt` (the IDE quick fix adds it), and a changed
  or removed one fails with RS0017.
- **The content hash must not change by accident.** Users log it as the prompt version. Only something that changes
  what the model receives or how it is called may change it; `PromptHasherTests` pins this.
- **New diagnostics** go in `PromptProblems` in Core with the next free TP number, plus a test and a row in the README
  table.
- **User-visible changes** get a line under `[Unreleased]` in `CHANGELOG.md`.
