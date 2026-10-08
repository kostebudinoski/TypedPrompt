using System.Text;

namespace TypedPrompt.Core;

/// <summary>Writes indented source code line by line. Shared by every emitter.</summary>
internal sealed class CodeWriter(string indentUnit = "    ")
{
    private readonly StringBuilder _text = new();
    private readonly Stack<string> _closers = new();
    private int _depth;

    /// <summary>Writes a line at the current indentation (an empty call writes a blank line).</summary>
    public void Line(string text = "")
    {
        if (text.Length > 0)
        {
            for (var i = 0; i < _depth; i++)
            {
                _text.Append(indentUnit);
            }

            _text.Append(text);
        }

        _text.Append('\n');
    }

    /// <summary>Writes a line one level deeper than the current indentation.</summary>
    public void Indented(string text)
    {
        _depth++;
        Line(text);
        _depth--;
    }

    /// <summary>Writes <paramref name="header"/> (if any) and an opening brace, then indents until <see cref="Close"/>.</summary>
    public void Open(string header, string trailing = "}")
    {
        if (header.Length > 0)
        {
            Line(header);
        }

        Line("{");
        _closers.Push(trailing);
        _depth++;
    }

    /// <summary>Indents following lines one level, without braces (for indentation-based languages such as F#).</summary>
    public void Push() => _depth++;

    /// <summary>Ends the indentation started with <see cref="Push"/>.</summary>
    public void Pop() => _depth--;

    /// <summary>Ends the innermost block opened with <see cref="Open"/>.</summary>
    public void Close()
    {
        _depth--;
        Line(_closers.Pop());
    }

    /// <inheritdoc />
    public override string ToString() => _text.ToString();
}
