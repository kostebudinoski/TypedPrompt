namespace TypedPrompt.Tests;

public class RenderedPromptTests
{
    [Fact]
    public void System_and_user_become_two_messages_in_order()
    {
        var rendered = new RenderedPrompt("ticket-summarize", "Answer in English.", "Printer is on fire");

        Assert.Equal(
            [new PromptMessage(PromptRole.System, "Answer in English."), new PromptMessage(PromptRole.User, "Printer is on fire")],
            rendered.Messages);
        Assert.Equal("Answer in English.\n\nPrinter is on fire", rendered.Text);
    }

    [Fact]
    public void Without_system_there_is_only_the_user_message()
    {
        var rendered = new RenderedPrompt("greeting", null, "Hello Anna!");

        Assert.Equal([new PromptMessage(PromptRole.User, "Hello Anna!")], rendered.Messages);
        Assert.Equal("Hello Anna!", rendered.Text);
    }

    [Fact]
    public void Model_and_schema_travel_with_the_rendered_prompt()
    {
        var model = new ModelSettings("claude-sonnet-5", Temperature: 0.2, MaxOutputTokens: 800, StopSequences: ["\n\nUser:"]);

        var rendered = new RenderedPrompt("ticket-summarize", null, "x", model, """{"type":"object"}""");

        Assert.Equal("claude-sonnet-5", rendered.Model?.Name);
        Assert.Equal(800, rendered.Model?.MaxOutputTokens);
        Assert.Equal("""{"type":"object"}""", rendered.OutputSchema);
    }

    [Fact]
    public void Examples_come_between_system_and_user_as_user_and_assistant_messages()
    {
        var rendered = new RenderedPrompt("ticket-summarize", "Summarise.", "VPN drops.")
        {
            Examples = [new PromptExample("Printer jammed.", "{\"priority\":\"normal\"}"), new PromptExample("Office down!", "{\"priority\":\"high\"}")],
        };

        Assert.Equal(
            [
                (PromptRole.System, "Summarise."),
                (PromptRole.User, "Printer jammed."),
                (PromptRole.Assistant, "{\"priority\":\"normal\"}"),
                (PromptRole.User, "Office down!"),
                (PromptRole.Assistant, "{\"priority\":\"high\"}"),
                (PromptRole.User, "VPN drops."),
            ],
            rendered.Messages.Select(m => (m.Role, m.Text)));
        Assert.Equal("Summarise.\n\nPrinter jammed.\n\n{\"priority\":\"normal\"}\n\nOffice down!\n\n{\"priority\":\"high\"}\n\nVPN drops.", rendered.Text);
    }

    [Fact]
    public void Examples_are_empty_by_default()
    {
        var rendered = new RenderedPrompt("greeting", null, "Hello!");

        Assert.Empty(rendered.Examples);
        Assert.Equal([new PromptMessage(PromptRole.User, "Hello!")], rendered.Messages);
    }

    [Fact]
    public void Model_settings_are_all_optional()
    {
        var model = new ModelSettings();

        Assert.Equal((null, null, null, null, null), (model.Name, model.Temperature, model.TopP, model.TopK, model.MaxOutputTokens));
        Assert.Null(model.StopSequences);
    }
}
