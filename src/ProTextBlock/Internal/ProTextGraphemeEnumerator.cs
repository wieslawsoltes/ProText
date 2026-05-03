using System.Globalization;

namespace ProTextBlock.Internal;

internal static class ProTextGraphemeEnumerator
{
    public static int Count(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return StringInfo.ParseCombiningCharacters(text).Length;
    }

    public static string TrimEndGraphemes(string text, int graphemeCount)
    {
        if (string.IsNullOrEmpty(text) || graphemeCount <= 0)
        {
            return string.Empty;
        }

        var indexes = StringInfo.ParseCombiningCharacters(text);

        if (graphemeCount >= indexes.Length)
        {
            return text;
        }

        return text[..indexes[graphemeCount]];
    }

    public static string RemoveLastGrapheme(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var indexes = StringInfo.ParseCombiningCharacters(text);
        return indexes.Length <= 1 ? string.Empty : text[..indexes[^1]];
    }
}