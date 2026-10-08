# TypedPrompt

Write your LLM prompts as `.prompt.toml` files; TypedPrompt turns each one into a **strongly typed C# class at build time**.
A missing value or a typo in a placeholder is a compile error, not a surprise in production.

```toml
# Prompts/ticket-summarize.prompt.toml
system = "You are a support assistant. Answer in {{language}}."
user   = "Summarise this ticket:\n{{ticket}}"

[variables.ticket]
type     = "text"
required = true

[variables.language]
type    = "text"
default = "English"

[model]
name        = "claude-sonnet-5"
temperature = 0.2
```

```csharp
var rendered = new TicketSummarizePrompt(ticket: "Login page is slow since Monday.").Render();

rendered.Messages;   // [system, user]: map to any LLM SDK
rendered.Model;      // claude-sonnet-5, temperature 0.2
rendered.Version;    // "ticket-summarize#6cb228b5": log it with the answer
```

- Files in `Prompts/` are picked up automatically; the class goes into `<RootNamespace>.Prompts`.
- Variables are constructor parameters (`text` → `string`, `number` → `double`, `boolean` → `bool`); required ones are required.
- Optional: `[model]` settings (including `effort`), an `[output]` JSON schema, `[[examples]]` for few-shot, `[metadata]`, a `version` label.
- Mistakes are compiler errors at the file and line (`TP001`–`TP013`), e.g. `{{tikcet}} is not declared … Did you mean 'ticket'?`.
- No runtime parsing and no dependencies: the app only references the small `TypedPrompt.dll`.

Full documentation and samples: https://github.com/kostebudinoski/TypedPrompt
