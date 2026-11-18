using System;
using System.Globalization;
using System.Collections.Generic;

namespace DemonIdol.Launch
{
    /// <summary>
    /// Strict JSON document check. A leading brace is not enough: trailing commas,
    /// trailing junk, and broken strings fail.
    /// </summary>
    public static class JsonSyntax
    {
        public static bool IsValidDocument(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            int index = 0;
            Skip(text, ref index);
            if (!TryValue(text, ref index))
                return false;
            Skip(text, ref index);
            return index == text.Length;
        }

        public static bool TryObject(string text, out Dictionary<string, JsonToken> fields, out string error)
        {
            fields = null;
            error = null;
            if (!IsValidDocument(text))
            {
                error = "Document is not valid JSON.";
                return false;
            }

            int index = 0;
            Skip(text, ref index);
            if (index >= text.Length || text[index] != '{')
            {
                error = "Reward document must be a JSON object.";
                return false;
            }

            var parsed = new Dictionary<string, JsonToken>(StringComparer.Ordinal);
            index++;
            Skip(text, ref index);
            if (index < text.Length && text[index] == '}')
            {
                error = "Reward document is missing required fields.";
                return false;
            }

            while (index < text.Length)
            {
                if (text[index] != '"')
                {
                    error = "Object key must be a string.";
                    return false;
                }
                if (!TryString(text, ref index, out string key))
                {
                    error = "Object key is not a valid string.";
                    return false;
                }
                if (parsed.ContainsKey(key))
                {
                    error = "Duplicate field " + key + ".";
                    return false;
                }
                Skip(text, ref index);
                if (index >= text.Length || text[index] != ':')
                {
                    error = "Missing colon after " + key + ".";
                    return false;
                }
                index++;
                Skip(text, ref index);
                int valueStart = index;
                if (!TryValue(text, ref index))
                {
                    error = "Invalid value for " + key + ".";
                    return false;
                }
                parsed.Add(key, new JsonToken(text.Substring(valueStart, index - valueStart).Trim()));
                Skip(text, ref index);
                if (index < text.Length && text[index] == ',')
                {
                    index++;
                    Skip(text, ref index);
                    if (index < text.Length && text[index] == '}')
                    {
                        error = "Trailing comma.";
                        return false;
                    }
                    continue;
                }
                if (index < text.Length && text[index] == '}')
                {
                    index++;
                    Skip(text, ref index);
                    if (index != text.Length)
                    {
                        error = "Trailing junk after object.";
                        return false;
                    }
                    fields = parsed;
                    return true;
                }
                error = "Object was not closed.";
                return false;
            }

            error = "Object was not closed.";
            return false;
        }

        static bool TryValue(string text, ref int index)
        {
            if (index >= text.Length)
                return false;
            char c = text[index];
            if (c == '"')
                return TryString(text, ref index, out _);
            if (c == '{')
                return TryContainer(text, ref index, '{', '}');
            if (c == '[')
                return TryContainer(text, ref index, '[', ']');
            if (c == 't')
                return ConsumeLiteral(text, ref index, "true");
            if (c == 'f')
                return ConsumeLiteral(text, ref index, "false");
            if (c == 'n')
                return ConsumeLiteral(text, ref index, "null");
            return TryNumber(text, ref index);
        }

        static bool TryContainer(string text, ref int index, char open, char close)
        {
            if (index >= text.Length || text[index] != open)
                return false;
            index++;
            Skip(text, ref index);
            if (index < text.Length && text[index] == close)
            {
                index++;
                return true;
            }

            while (index < text.Length)
            {
                if (open == '{')
                {
                    if (!TryString(text, ref index, out _))
                        return false;
                    Skip(text, ref index);
                    if (index >= text.Length || text[index] != ':')
                        return false;
                    index++;
                    Skip(text, ref index);
                }
                if (!TryValue(text, ref index))
                    return false;
                Skip(text, ref index);
                if (index < text.Length && text[index] == ',')
                {
                    index++;
                    Skip(text, ref index);
                    if (index < text.Length && text[index] == close)
                        return false;
                    continue;
                }
                if (index < text.Length && text[index] == close)
                {
                    index++;
                    return true;
                }
                return false;
            }
            return false;
        }

        static bool TryString(string text, ref int index, out string value)
        {
            value = null;
            if (index >= text.Length || text[index] != '"')
                return false;
            index++;
            var built = new System.Text.StringBuilder();
            while (index < text.Length)
            {
                char c = text[index];
                if (c == '"')
                {
                    index++;
                    value = built.ToString();
                    return true;
                }
                if (c == '\\')
                {
                    index++;
                    if (index >= text.Length)
                        return false;
                    char esc = text[index];
                    index++;
                    switch (esc)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            built.Append(esc);
                            break;
                        case 'b': built.Append('\b'); break;
                        case 'f': built.Append('\f'); break;
                        case 'n': built.Append('\n'); break;
                        case 'r': built.Append('\r'); break;
                        case 't': built.Append('\t'); break;
                        case 'u':
                            if (index + 4 > text.Length)
                                return false;
                            string hex = text.Substring(index, 4);
                            if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code))
                                return false;
                            built.Append((char)code);
                            index += 4;
                            break;
                        default:
                            return false;
                    }
                    continue;
                }
                if (c < 0x20)
                    return false;
                built.Append(c);
                index++;
            }
            return false;
        }

        static bool TryNumber(string text, ref int index)
        {
            int start = index;
            if (index < text.Length && text[index] == '-')
                index++;
            if (index >= text.Length || !char.IsDigit(text[index]))
                return false;
            if (text[index] == '0')
            {
                index++;
            }
            else
            {
                while (index < text.Length && char.IsDigit(text[index]))
                    index++;
            }
            if (index < text.Length && text[index] == '.')
            {
                index++;
                if (index >= text.Length || !char.IsDigit(text[index]))
                    return false;
                while (index < text.Length && char.IsDigit(text[index]))
                    index++;
            }
            if (index < text.Length && (text[index] == 'e' || text[index] == 'E'))
            {
                index++;
                if (index < text.Length && (text[index] == '+' || text[index] == '-'))
                    index++;
                if (index >= text.Length || !char.IsDigit(text[index]))
                    return false;
                while (index < text.Length && char.IsDigit(text[index]))
                    index++;
            }
            return index > start;
        }

        static bool ConsumeLiteral(string text, ref int index, string literal)
        {
            if (index + literal.Length > text.Length)
                return false;
            if (string.CompareOrdinal(text, index, literal, 0, literal.Length) != 0)
                return false;
            index += literal.Length;
            return true;
        }

        static void Skip(string text, ref int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;
        }
    }

    public readonly struct JsonToken
    {
        public readonly string Raw;

        public JsonToken(string raw)
        {
            Raw = raw;
        }
    }
}
