using SkiaSharp;

namespace ProText.Core;

internal static class ProTextRenderFontCache
{
    private const int MaxEntries = 128;
    private static readonly object s_sync = new();
    private static readonly Dictionary<string, ProTextRenderFontEntry> s_entries = new(StringComparer.Ordinal);
    private static readonly Queue<string> s_order = new();

    public static ProTextRenderFontLease Get(ProTextRichStyle style)
    {
        var key = style.FontDescriptor;

        lock (s_sync)
        {
            if (s_entries.TryGetValue(key, out var cached))
            {
                cached.AddRef();
                return new ProTextRenderFontLease(cached);
            }

            var created = Create(style);
            created.AddRef();
            s_entries.Add(key, created);
            s_order.Enqueue(key);
            Trim();
            return new ProTextRenderFontLease(created);
        }
    }

    public static double MeasureText(string text, ProTextRichStyle style)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        using var renderFontLease = Get(style);
        var renderFont = renderFontLease.Font;
        var width = style.LetterSpacing.Equals(0d) && renderFont.Typeface.ContainsGlyphs(text)
            ? renderFont.Font.MeasureText(text)
            : MeasureTextWithFallback(text, style, renderFont);
        var spacingCount = Math.Max(0, ProTextGraphemeEnumerator.Count(text) - 1);
        return Math.Max(0, width + spacingCount * style.LetterSpacing);
    }

    public static void Clear()
    {
        lock (s_sync)
        {
            foreach (var entry in s_entries.Values)
            {
                entry.MarkEvicted();
            }

            s_entries.Clear();
            s_order.Clear();
        }
    }

    private static ProTextRenderFontEntry Create(ProTextRichStyle style)
    {
        var family = ProTextFontDescriptor.GetPrimaryFamilyName(style.FontFamily);
        var fontStyle = ProTextFontResolver.CreateFontStyle(style.FontWeight, style.FontStretch, style.FontStyle);
        var resolvedTypeface = ProTextFontResolver.ResolveTypeface(
            style.FontFamily,
            style.FontWeight,
            style.FontStretch,
            style.FontStyle);
        var font = ProTextFontResolver.CreateFont(resolvedTypeface.Typeface, style.FontSize, resolvedTypeface.Simulations);

        return new ProTextRenderFontEntry(new ProTextRenderFont(family, fontStyle, resolvedTypeface.Typeface, resolvedTypeface.OwnsTypeface, font));
    }

    private static void Trim()
    {
        while (s_entries.Count > MaxEntries && s_order.Count > 0)
        {
            var key = s_order.Dequeue();
            if (s_entries.Remove(key, out var entry))
            {
                entry.MarkEvicted();
            }
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

internal readonly struct ProTextRenderFontLease : IDisposable
{
    private readonly ProTextRenderFontEntry? _entry;

    public ProTextRenderFontLease(ProTextRenderFontEntry entry)
    {
        _entry = entry;
        Font = entry.Font;
    }

    public ProTextRenderFont Font { get; }

    public void Dispose()
    {
        _entry?.Release();
    }
}

internal sealed class ProTextRenderFontEntry
{
    private int _refCount;
    private int _evicted;
    private int _disposed;

    public ProTextRenderFontEntry(ProTextRenderFont font)
    {
        Font = font;
    }

    public ProTextRenderFont Font { get; }

    public void AddRef()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(ProTextRenderFont));
        }

        Interlocked.Increment(ref _refCount);
    }

    public void Release()
    {
        if (Interlocked.Decrement(ref _refCount) == 0 && Volatile.Read(ref _evicted) != 0)
        {
            DisposeFont();
        }
    }

    public void MarkEvicted()
    {
        Volatile.Write(ref _evicted, 1);

        if (Volatile.Read(ref _refCount) == 0)
        {
            DisposeFont();
        }
    }

    private void DisposeFont()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            Font.Dispose();
        }
    }
}

internal sealed class ProTextRenderFont : IDisposable
{
    public ProTextRenderFont(string family, SKFontStyle fontStyle, SKTypeface typeface, SKFont font)
        : this(family, fontStyle, typeface, ownsTypeface: true, font)
    {
    }

    public ProTextRenderFont(string family, SKFontStyle fontStyle, SKTypeface typeface, bool ownsTypeface, SKFont font)
    {
        Family = family;
        FontStyle = fontStyle;
        Typeface = typeface;
        OwnsTypeface = ownsTypeface;
        Font = font;
    }

    public string Family { get; }

    public SKFontStyle FontStyle { get; }

    public SKTypeface Typeface { get; }

    public bool OwnsTypeface { get; }

    public SKFont Font { get; }

    public void Dispose()
    {
        Font.Dispose();

        if (OwnsTypeface)
        {
            Typeface.Dispose();
        }
    }
}
