using Sample.Prompts;
using TypedPrompt;

Section("1. Extract structured data from a document (invoice-extract.prompt.toml)");
Print(new InvoiceExtractPrompt(invoiceText: """
    ACME Office Supplies — Invoice INV-2026-0412
    Date: 03.10.2026, payable within 30 days
    2 x Ergonomic chair ........ 398.00
    1 x Standing desk .......... 549.00
    Total: 947.00 CHF
    """).Render());

Section("2. Classify with few-shot examples (sentiment-classify.prompt.toml)");
var review = new SentimentClassifyPrompt(review: "Great sound, but the battery barely lasts a day.");
Print(review.Render());
Console.WriteLine($"with a reason: {new SentimentClassifyPrompt(review.Review, explainHint: " Then give one short reason.").Render().System}");

Section("3. A developer tool with typed options (code-review.prompt.toml)");
Print(new CodeReviewPrompt(
    diff: """
        --- a/OrderService.cs
        +++ b/OrderService.cs
        @@ -12,6 +12,7 @@ public decimal Total(Order order)
        -    return order.Lines.Sum(l => l.Price * l.Quantity);
        +    var total = order.Lines.Sum(l => l.Price * l.Quantity);
        +    return total - order.Discount;
        """,
    title: "Apply order discount to the total",
    maxComments: 5,          // number → double
    requireTests: true)      // boolean → bool
    .Render());

Section("4. Any prompt through the common interface");
ITypedPrompt[] prompts =
[
    new InvoiceExtractPrompt(invoiceText: "Invoice 17, total 12.50", currency: "USD"),
    new SentimentClassifyPrompt(review: "Okay, I guess."),
    new CodeReviewPrompt(diff: "+ Console.WriteLine(password);", title: "Debug logging", language: "C#"),
];
foreach (var prompt in prompts)
{
    var rendered = prompt.Render();
    Console.WriteLine($"{rendered.Version,-34} {rendered.Messages.Count} message(s), model {rendered.Model?.Name}");
}

Section("5. The definition, without any values");
Describe(CodeReviewPrompt.Definition);

static void Print(RenderedPrompt rendered)
{
    foreach (var message in rendered.Messages)
    {
        Console.WriteLine($"[{message.Role.ToString().ToLowerInvariant()}] {message.Text}");
    }

    if (rendered.Model is { } model)
    {
        var effort = model.Effort is null ? string.Empty : $", effort {model.Effort}";
        Console.WriteLine($"model:  {model.Name} (temperature {model.Temperature}, max output {model.MaxOutputTokens} tokens{effort})");
    }

    Console.WriteLine($"output: {(rendered.OutputSchema is null ? "text" : "json, schema of " + rendered.OutputSchema.Length + " chars")}");
    Console.WriteLine($"version: {rendered.Version}   ← log this with the model's answer");
}

static void Describe(PromptDefinition definition)
{
    Console.WriteLine($"key:         {definition.Key}");
    Console.WriteLine($"version:     {definition.Version} (label {definition.VersionLabel})");
    Console.WriteLine($"description: {definition.Description}");
    Console.WriteLine($"tags:        {string.Join(", ", definition.Tags)}");
    Console.WriteLine($"metadata:    {string.Join(", ", definition.Metadata.Select(p => $"{p.Key}={p.Value}"))}");
    Console.WriteLine($"model:       {definition.Model?.Name}, effort {definition.Model?.Effort}, stop {definition.Model?.StopSequences?.Count} sequence(s)");
    foreach (var variable in definition.Variables)
    {
        var detail = variable.Required ? "required" : variable.Default is null ? "optional" : $"default {variable.Default}";
        Console.WriteLine($"variable:    {variable.Name} ({variable.Type.ToString().ToLowerInvariant()}, {detail})");
    }
}

static void Section(string title) => Console.WriteLine($"{Environment.NewLine}== {title} ==");
