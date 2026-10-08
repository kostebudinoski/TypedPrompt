namespace TypedPrompt.Core.Tests;

public class TemplateParserTests
{
    [Fact]
    public void Text_without_placeholders_is_one_literal()
    {
        var (segments, errors) = TemplateParser.Parse("Just text } and }} too");

        Assert.Empty(errors);
        Assert.Equal([TemplateSegment.Literal("Just text } and }} too", 0)], segments);
    }

    [Fact]
    public void Placeholders_allow_spaces_and_record_their_offset()
    {
        var (segments, errors) = TemplateParser.Parse("Hi {{ name }}, {{tone}}!");

        Assert.Empty(errors);
        Assert.Equal(
            [
                TemplateSegment.Literal("Hi ", 0),
                TemplateSegment.Placeholder("name", 3),
                TemplateSegment.Literal(", ", 13),
                TemplateSegment.Placeholder("tone", 15),
                TemplateSegment.Literal("!", 23),
            ],
            segments);
    }

    [Fact]
    public void Adjacent_placeholders_have_no_empty_literal_between_them()
    {
        var (segments, _) = TemplateParser.Parse("{{a}}{{b}}");

        Assert.Equal([TemplateSegment.Placeholder("a", 0), TemplateSegment.Placeholder("b", 5)], segments);
    }

    [Fact]
    public void Backslash_before_braces_writes_them_literally()
    {
        var (segments, errors) = TemplateParser.Parse(@"Write \{{name}} to get {{name}}");

        Assert.Empty(errors);
        Assert.Equal("Write {{name}} to get ", segments[0].Value);
        Assert.Equal(TemplateSegment.Placeholder("name", 23), segments[1]);
    }

    [Fact]
    public void A_backslash_elsewhere_is_ordinary_text()
    {
        var (segments, _) = TemplateParser.Parse(@"C:\path\to {{x}}");

        Assert.Equal(@"C:\path\to ", segments[0].Value);
    }

    [Theory]
    [InlineData("Hi {{name", 3)]
    [InlineData("Hi {{na me}}", 3)]
    [InlineData("{{1x}} ok", 0)]
    public void Errors_record_where_the_placeholder_starts(string text, int offset)
    {
        var (_, errors) = TemplateParser.Parse(text);

        Assert.Equal(offset, Assert.Single(errors).Offset);
    }

    [Fact]
    public void Parsing_continues_after_an_error()
    {
        var (segments, errors) = TemplateParser.Parse("{{bad name}} then {{good}}");

        Assert.Single(errors);
        Assert.Contains(TemplateSegment.Placeholder("good", 18), segments);
    }
}
