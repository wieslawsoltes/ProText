using Avalonia.Media;
using SkiaSharp;

namespace ProTextBlock.Internal;

internal static class ProTextRenderFontCache
{
    private const int MaxEntries = 128;
    private static readonly object s_sync = new();
    private static readonly Dictionary<string, ProTextRenderFont> s_entries = new(StringComparer.Ordinal);
    private static readonly Queue<string> s_order = new();

    public static ProTextRenderFont Get(ProTextRichStyle style)
    {
        var key = style.FontDescriptor;

        lock (s_sync)
        {
            if (s_entries.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var created = Create(style);
            s_entries.Add(key, created);
            s_order.Enqueue(key);
            Trim();
            return created;
        }
    }

    public static double MeasureText(string text, ProTextRichStyle style)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var renderFont = Get(style);
        var width = style.LetterSpacing.Equals(0d) && renderFont.Typeface.ContainsGlyphs(text)
            ? renderFont.Font.MeasureText(text)
            : MeasureTextWithFallback(text, style, renderFont);
        var spacingCount = Math.Max(0, ProTextGraphemeEnumerator.Count(text) - 1);
        return Math.Max(0, width + spacingCount * style.LetterSpacing);
    }

    private static ProTextRenderFont Create(ProTextRichStyle style)
    {
        var family = ProTextBlockFontDescriptor.GetPrimaryFamilyName(style.FontFamily);
        var fontStyle = ProTextFontResolver.CreateFontStyle(style.FontWeight, style.FontStretch, style.FontStyle);
        var resolvedTypeface = ProTextFontResolver.ResolveTypeface(
            style.FontFamily,
            style.FontWeight,
            style.FontStretch,
            style.FontStyle);
        var font = ProTextFontResolver.CreateFont(resolvedTypeface.Typeface, style.FontSize, resolvedTypeface.Simulations);

        return new ProTextRenderFont(family, fontStyle, resolvedTypeface.Typeface, font);
    }

    private static void Trim()
    {
        while (s_entries.Count > MaxEntries && s_order.Count > 0)
        {
            var key = s_order.Dequeue();
            s_entries.Remove(key);
        }
    }

    private static double MeasureTextWithFallback(string text, ProTextRichStyle style, ProTextRenderFont renderFont)
    {
        var width = 0d;

        foreach (var grapheme in ProTextFontResolver.EnumerateGraphemes(text))
        {
            using var resolved = ProTextFontResolver.ResolveTypeface(renderFont.Typeface, renderFont.Family, renderFont.FontStyle, grapheme);
            using var font = ReferenceEquals(resolved.Typeface, renderFont.Typeface)
                ? null
                : ProTextFontResolver.CreateFont(resolved.Typeface, style.FontSize);
            width += (font ?? renderFont.Font).MeasureText(grapheme);
        }

        return width;
    }
}

internal sealed class ProTextRenderFont
{
    public ProTextRenderFont(string family, SKFontStyle fontStyle, SKTypeface typeface, SKFont font)
    {
        Family = family;
        FontStyle = fontStyle;
        Typeface = typeface;
        Font = font;
    }

    public string Family { get; }

    public SKFontStyle FontStyle { get; }

    public SKTypeface Typeface { get; }

    public SKFont Font { get; }
}