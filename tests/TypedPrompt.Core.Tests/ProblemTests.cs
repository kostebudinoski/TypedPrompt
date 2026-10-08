namespace TypedPrompt.Core.Tests;

/// <summary>Every problem id, reported at the right line with a useful message.</summary>
public class ProblemTests
{
    private const string Valid = """
        user = "Hello {{name}}!"

        [variables.name]
        type     = "text"
        required = true
        """;

    [Fact]
    public void Valid_file_has_no_problems()
    {
        var result = Parse(Valid);

        Assert.Empty(result.Problems);
        Assert.NotNull(result.Prompt);
    }

    [Fact]
    public void TP001_invalid_toml_points_at_the_line()
    {
        var problem = Single(Parse("description = \"ok\"\nuser = \"never closed\n"), "TP001");

        Assert.Equal(2, problem.Line);
    }

    [Fact]
    public void TP001_backslash_before_braces_in_a_basic_string_gets_a_hint()
    {
        var problem = Single(Parse("user = \"Write \\{{ here\"\n"), "TP001");

        Assert.Contains("'''", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TP002_unknown_field_suggests_the_known_one()
    {
        var problem = Single(Parse(Valid + "\ndescripton = \"typo\"\n"), "TP002");

        Assert.Equal(6, problem.Line);
        Assert.Contains("Did you mean 'description'?", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TP002_unknown_field_inside_a_variable()
    {
        var problem = Single(Parse(Valid + "\nrequried = true\n"), "TP002");

        Assert.Equal(6, problem.Line);
        Assert.Contains("[variables.name]", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TP003_user_is_required()
    {
        var result = Parse("system = \"You are helpful.\"\n");

        Assert.Equal("TP003", Assert.Single(result.Problems).Id);
        Assert.Null(result.Prompt);
    }

    [Fact]
    public void TP004_undeclared_placeholder_points_at_its_line_inside_a_multiline_template()
    {
        var result = Parse("""
            user = '''
            Ticket:
            {{tikcet}}
            '''

            [variables.ticket]
            type = "text"
            """);

        var problem = Single(result, "TP004");
        Assert.Equal((3, 1), (problem.Line, problem.Column));
        Assert.Contains("Did you mean 'ticket'?", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TP004_on_a_single_line_template_points_at_the_column()
    {
        var problem = Single(Parse("user = \"Hi {{nmae}}\"\n\n[variables.name]\ntype = \"text\"\n"), "TP004");

        Assert.Equal((1, 12), (problem.Line, problem.Column));
    }

    [Fact]
    public void TP004_after_an_escaped_newline_stays_on_the_line_it_is_written()
    {
        var problem = Single(Parse("description = \"x\"\nuser = \"Summarise:\\n{{tikcet}}\"\n\n[variables.ticket]\ntype = \"text\"\n"), "TP004");

        Assert.Equal((2, 21), (problem.Line, problem.Column));
    }

    [Fact]
    public void TP004_in_a_multiline_basic_string_with_escapes_points_at_the_placeholder()
    {
        var problem = Single(Parse("user = \"\"\"\nTab\\there\nand {{nmae}}\n\"\"\"\n\n[variables.name]\ntype = \"text\"\n"), "TP004");

        Assert.Equal((3, 5), (problem.Line, problem.Column));
    }

    [Fact]
    public void TP005_unused_variable_is_a_warning_and_the_prompt_is_still_produced()
    {
        var result = Parse(Valid + "\n[variables.unused]\ntype = \"text\"\n");

        var problem = Single(result, "TP005");
        Assert.Equal(ProblemSeverity.Warning, problem.Severity);
        Assert.Equal(6, problem.Line);
        Assert.NotNull(result.Prompt);
    }

    [Theory]
    [InlineData("[variables.name]\ntype = \"string\"\nrequired = true\n", "'string' is not a valid type")]
    [InlineData("[variables.name]\nrequired = true\n", "needs a type")]
    [InlineData("[variables.name]\ntype = \"number\"\ndefault = \"three\"\n", "must be a number")]
    [InlineData("[variables.name]\ntype = \"boolean\"\ndefault = 1\n", "must be true or false")]
    public void TP006_invalid_variables(string variable, string expected)
    {
        var problem = Single(Parse("user = \"Hello {{name}}!\"\n\n" + variable), "TP006");

        Assert.Contains(expected, problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TP006_invalid_variable_name()
    {
        var problem = Single(Parse("user = \"Hello\"\n\n[variables.\"bad-name\"]\ntype = \"text\"\n"), "TP006");

        Assert.Contains("'bad-name' is not a valid variable name", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TP007_required_with_default_is_a_warning()
    {
        var problem = Single(Parse(Valid + "\ndefault = \"Anna\"\n"), "TP007");

        Assert.Equal(ProblemSeverity.Warning, problem.Severity);
    }

    [Fact]
    public void TP008_same_class_name_or_key_in_two_files()
    {
        var a = PromptFileParser.Parse("prompts/support-reply.prompt.toml", Valid).Prompt!;
        var b = PromptFileParser.Parse("prompts/support_reply.prompt.toml", Valid).Prompt!;
        var c = PromptFileParser.Parse("prompts/other.prompt.toml", "key = \"support-reply\"\n" + Valid).Prompt!;

        var problems = PromptSetValidator.Validate([a, b, c]);

        Assert.Equal(2, problems.Count(p => p.Id == "TP008" && p.Message.Contains("class name 'SupportReply'", StringComparison.Ordinal)));
        Assert.Equal(2, problems.Count(p => p.Id == "TP008" && p.Message.Contains("key 'support-reply'", StringComparison.Ordinal)));
    }

    [Fact]
    public void TP008_class_names_that_differ_only_in_case()
    {
        var a = PromptFileParser.Parse("prompts/fooBar.prompt.toml", Valid).Prompt!;
        var b = PromptFileParser.Parse("prompts/old/foobar.prompt.toml", Valid).Prompt!;

        var problems = PromptSetValidator.Validate([a, b]);

        Assert.Equal(2, problems.Count);
        Assert.All(problems, p => Assert.Contains("'FooBar' and 'Foobar'", p.Message, StringComparison.Ordinal));
        Assert.All(problems, p => Assert.Contains("differ only in upper/lower case", p.Message, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("user = \"Hello {{name\"", "never closed")]
    [InlineData("user = \"Hello {{ 1name }}\"", "not a valid placeholder")]
    [InlineData("user = \"Hello {{}}\"", "not a valid placeholder")]
    public void TP009_invalid_placeholders(string user, string expected)
    {
        var problem = Single(Parse(user + "\n"), "TP009");

        Assert.Contains(expected, problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TP010_schema_that_is_not_json_points_at_its_line()
    {
        var result = Parse(Valid + """

            [output]
            schema = '''
            {
              "type": "object",
              "properties": { "a": { "type": "string" }, }
            }
            '''
            """);

        var problem = Single(result, "TP010");
        Assert.Equal(10, problem.Line);
    }

    [Theory]
    [InlineData("[model]\ntemperature = \"hot\"\n", "model.temperature", "'temperature' must be a number 0 or more")]
    [InlineData("[model]\ntop_p = 1.5\n", "model.top_p", "from 0 to 1")]
    [InlineData("[model]\ntop_k = 0\n", "model.top_k", "whole number of 1 or more")]
    [InlineData("[model]\nmax_output_tokens = 2.5\n", "model.max_output_tokens", "whole number of 1 or more")]
    [InlineData("[model]\nstop_sequences = \"END\"\n", "model.stop_sequences", "list of text")]
    [InlineData("[model]\neffort = \"hgih\"\n", "model.effort", "'hgih' is not a valid effort. Did you mean 'high'?")]
    [InlineData("[model]\neffort = \"extreme\"\n", "model.effort", "Use one of: minimal, low, medium, high, max")]
    [InlineData("[model]\neffort = 3\n", "model.effort", "'effort' must be text")]
    [InlineData("[metadata]\nowner = 42\n", "metadata.owner", "must be text")]
    [InlineData("[output]\nformat = \"json\"\n", "output", "needs a schema")]
    public void TP011_wrong_values(string section, string _, string expected)
    {
        var problem = Single(Parse(Valid + "\n" + section), "TP011");

        Assert.Contains(expected, problem.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("tags = \"support\"\n", "list of text")]
    [InlineData("key = \"has spaces\"\n", "not a valid key")]
    [InlineData("description = 42\n", "must be text")]
    public void TP011_wrong_top_level_values(string field, string expected)
    {
        var problem = Single(Parse(field + Valid), "TP011");

        Assert.Equal(1, problem.Line);
        Assert.Contains(expected, problem.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("minimal")]
    [InlineData("low")]
    [InlineData("medium")]
    [InlineData("high")]
    [InlineData("max")]
    public void Every_effort_level_is_accepted(string effort)
    {
        var result = Parse(Valid + $"\n[model]\neffort = \"{effort}\"\n");

        Assert.Empty(result.Problems);
        Assert.Equal(effort, result.Prompt?.Model?.Effort);
    }

    [Fact]
    public void Effort_is_optional()
    {
        var result = Parse(Valid + "\n[model]\nname = \"claude-sonnet-5\"\n");

        Assert.Empty(result.Problems);
        Assert.Null(result.Prompt?.Model?.Effort);
    }

    private const string WithExamples = """
        user = "Hello {{name}}!"

        [[examples]]
        user      = "Hello Bob!"
        assistant = "Hi Bob."

        [[examples]]
        user      = "Hello {{nmae}}!"
        assistant = "Hi."

        [variables.name]
        type     = "text"
        required = true
        """;

    [Fact]
    public void Examples_are_read_in_order_and_their_placeholders_are_checked()
    {
        var result = Parse(WithExamples.Replace("{{nmae}}", "{{name}}", StringComparison.Ordinal));

        Assert.Empty(result.Problems);
        Assert.Equal(["Hello Bob!", "Hello {{name}}!"], result.Prompt!.Examples.Select(e => e.User.Text));
    }

    [Fact]
    public void TP004_in_the_second_example_points_at_the_second_example()
    {
        var problem = Single(Parse(WithExamples), "TP004");

        Assert.Equal(8, problem.Line);
    }

    [Fact]
    public void TP002_unknown_field_in_an_example_points_at_that_example()
    {
        var problem = Single(Parse(WithExamples.Replace("{{nmae}}", "{{name}}", StringComparison.Ordinal).Replace("assistant = \"Hi.\"", "asistant = \"Hi.\"", StringComparison.Ordinal)), "TP002");

        Assert.Equal(9, problem.Line);
        Assert.Contains("in [[examples]]. Did you mean 'assistant'?", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TP011_example_without_an_answer()
    {
        var result = Parse("user = \"Hi\"\n\n[[examples]]\nuser = \"Hello\"\n");

        var problem = Single(result, "TP011");
        Assert.Equal(3, problem.Line);
        Assert.Contains("needs a user text and the assistant answer", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TP011_examples_that_are_not_sections()
    {
        var problem = Single(Parse("user = \"Hi\"\nexamples = \"none\"\n"), "TP011");

        Assert.Contains("[[examples]]", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Examples_can_also_be_an_inline_array()
    {
        var result = Parse("user = \"Hi\"\nexamples = [ { user = \"a\", assistant = \"b\" }, { user = \"c\", assistant = \"d\" } ]\n");

        Assert.Empty(result.Problems);
        Assert.Equal(["b", "d"], result.Prompt!.Examples.Select(e => e.Assistant.Text));
    }

    [Fact]
    public void A_variable_used_only_in_an_example_is_not_unused()
    {
        var result = Parse("user = \"Hi\"\n\n[[examples]]\nuser = \"{{name}}\"\nassistant = \"ok\"\n\n[variables.name]\ntype = \"text\"\n");

        Assert.Empty(result.Problems);
    }

    [Fact]
    public void User_written_below_a_section_gets_a_hint()
    {
        var result = Parse("system = \"Be nice.\"\n\n[[examples]]\nuser = \"a\"\nassistant = \"b\"\n\nuser = \"Real request\"\n");

        Assert.Contains(result.Problems, p => p.Message.Contains("above the first [section]", StringComparison.Ordinal));
    }

    [Fact]
    public void TP012_file_name_without_letters()
    {
        var result = PromptFileParser.Parse("prompts/123.prompt.toml", Valid);

        Assert.Equal("TP012", Assert.Single(result.Problems).Id);
    }

    [Fact]
    public void All_problems_are_reported_at_once_in_file_order()
    {
        var result = Parse("""
            usr = "typo"
            user = "Hi {{nmae}} {{ bad name }}"

            [variables.name]
            type = "string"
            """);

        Assert.Equal(["TP002", "TP004", "TP009", "TP006"], result.Problems.Select(p => p.Id));
        Assert.Null(result.Prompt);
    }

    [Fact]
    public void Problems_format_like_compiler_messages()
    {
        var problem = Single(Parse("user = \"Hi {{nmae}}\"\n"), "TP004");

        Assert.StartsWith("prompts/test.prompt.toml(1,12): error TP004: {{nmae}} is not declared", problem.ToString(), StringComparison.Ordinal);
    }

    private static PromptParseResult Parse(string toml) => PromptFileParser.Parse("prompts/test.prompt.toml", toml);

    private static PromptProblem Single(PromptParseResult result, string id)
    {
        var matches = result.Problems.Where(p => p.Id == id).ToList();
        Assert.True(matches.Count == 1, $"Expected one {id}, got:\n{string.Join("\n", result.Problems)}");
        return matches[0];
    }
}
