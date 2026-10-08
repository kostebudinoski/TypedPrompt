namespace TypedPrompt.Core;

/// <summary>
/// Checks that a text is well-formed JSON (RFC 8259), without a JSON library: Core runs inside the compiler, where
/// adding System.Text.Json is risky. It only checks syntax; it does not validate JSON Schema rules.
/// </summary>
public static class JsonChecker
{
    private const int MaxDepth = 64;

    /// <summary>Returns <see langword="null"/> when <paramref name="json"/> is one well-formed JSON value, otherwise where and what is wrong.</summary>
    public static (int Offset, string Message)? Check(string json)
    {
        var reader = new Reader(json ?? string.Empty);
        try
        {
            reader.SkipWhitespace();
            reader.Value(0);
            reader.SkipWhitespace();
            return reader.AtEnd ? null : (reader.Position, "Unexpected text after the JSON value.");
        }
        catch (FormatException ex)
        {
            return (reader.Position, ex.Message);
        }
    }

    private sealed class Reader(string text)
    {
        public int Position { get; private set; }

        public bool AtEnd => Position >= text.Length;

        private char Current => AtEnd ? '\0' : text[Position];

        public void SkipWhitespace()
        {
            while (!AtEnd && Current is ' ' or '\t' or '\n' or '\r')
            {
                Position++;
            }
        }

        public void Value(int depth)
        {
            if (depth > MaxDepth)
            {
                throw new FormatException("The JSON is nested too deeply.");
            }

            switch (Current)
            {
                case '{':
                    Object(depth);
                    break;
                case '[':
                    Array(depth);
                    break;
                case '"':
                    String();
                    break;
                case 't':
                    Word("true");
                    break;
                case 'f':
                    Word("false");
                    break;
                case 'n':
                    Word("null");
                    break;
                case '-' or (>= '0' and <= '9'):
                    Number();
                    break;
                default:
                    throw new FormatException(AtEnd ? "The JSON ends too early." : $"Unexpected '{Current}'.");
            }
        }

        private void Object(int depth)
        {
            Position++;
            SkipWhitespace();
            if (Current == '}')
            {
                Position++;
                return;
            }

            while (true)
            {
                SkipWhitespace();
                if (Current != '"')
                {
                    throw new FormatException("Expected a property name in double quotes.");
                }

                String();
                SkipWhitespace();
                Expect(':');
                SkipWhitespace();
                Value(depth + 1);
                SkipWhitespace();
                if (Current == ',')
                {
                    Position++;
                    continue;
                }

                Expect('}');
                return;
            }
        }

        private void Array(int depth)
        {
            Position++;
            SkipWhitespace();
            if (Current == ']')
            {
                Position++;
                return;
            }

            while (true)
            {
                SkipWhitespace();
                Value(depth + 1);
                SkipWhitespace();
                if (Current == ',')
                {
                    Position++;
                    continue;
                }

                Expect(']');
                return;
            }
        }

        private void String()
        {
            Position++;
            while (!AtEnd)
            {
                var c = Current;
                if (c == '"')
                {
                    Position++;
                    return;
                }

                if (c < ' ')
                {
                    throw new FormatException("Control characters must be escaped in JSON strings.");
                }

                if (c == '\\')
                {
                    Position++;
                    if (Current == 'u')
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            Position++;
                            if (!Uri.IsHexDigit(Current))
                            {
                                throw new FormatException("Expected four hex digits after \\u.");
                            }
                        }
                    }
                    else if (Current is not ('"' or '\\' or '/' or 'b' or 'f' or 'n' or 'r' or 't'))
                    {
                        throw new FormatException($"'\\{Current}' is not a valid escape.");
                    }
                }

                Position++;
            }

            throw new FormatException("A string is never closed.");
        }

        private void Number()
        {
            if (Current == '-')
            {
                Position++;
            }

            if (Current == '0')
            {
                Position++;
            }
            else
            {
                Digits();
            }

            if (Current == '.')
            {
                Position++;
                Digits();
            }

            if (Current is 'e' or 'E')
            {
                Position++;
                if (Current is '+' or '-')
                {
                    Position++;
                }

                Digits();
            }
        }

        private void Digits()
        {
            if (!(Current >= '0' && Current <= '9'))
            {
                throw new FormatException("Expected a digit.");
            }

            while (Current >= '0' && Current <= '9')
            {
                Position++;
            }
        }

        private void Word(string word)
        {
            if (string.CompareOrdinal(text, Position, word, 0, word.Length) != 0)
            {
                throw new FormatException($"Unexpected '{Current}'.");
            }

            Position += word.Length;
        }

        private void Expect(char c)
        {
            if (Current != c)
            {
                throw new FormatException(AtEnd ? $"Expected '{c}' but the JSON ends." : $"Expected '{c}' but found '{Current}'.");
            }

            Position++;
        }
    }
}
