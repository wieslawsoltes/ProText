using System.Globalization;
using Avalonia.Media;
using SkiaSharp;

namespace ProText.Internal;

internal static class ProTextFontResolver
{
    private static readonly string[] s_emptyBcp47 = [];

    public static SKFontStyle CreateFontStyle(FontWeight weight, FontStretch stretch, FontStyle style)
    {
        return new SKFontStyle((int)weight, (int)stretch, ToSkSlant(style));
    }

    public static SKFontStyle CreateFontStyle(int weight, FontStretch stretch, bool italic)
    {
        return new SKFontStyle(weight, (int)stretch, italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
    }

    public static SKTypeface CreateTypeface(string family, SKFontStyle style)
    {
        return SKTypeface.FromFamilyName(family, style) ?? SKTypeface.CreateDefault();
    }

    public static ResolvedTypeface ResolveTypeface(FontFamily family, FontWeight weight, FontStretch stretch, FontStyle style)
    {
        if (TryCreateTypefaceFromFontManager(new Typeface(family, style, weight, stretch), out var typeface, out var simulations))
        {
            return new ResolvedTypeface(typeface, ownsTypeface: true, simulations);
        }

        var familyName = ProTextFontDescriptor.GetPrimaryFamilyName(family);
        var skStyle = CreateFontStyle(weight, stretch, style);
        return new ResolvedTypeface(CreateTypeface(familyName, skStyle), ownsTypeface: true, FontSimulations.None);
    }

    public static SKFont CreateFont(SKTypeface typeface, double fontSize, FontSimulations simulations = FontSimulations.None)
    {
        return new SKFont(typeface, (float)fontSize, skewX: (simulations & FontSimulations.Oblique) != 0 ? -0.3f : 0f)
        {
            Subpixel = true,
            LinearMetrics = true,
            Embolden = (simulations & FontSimulations.Bold) != 0,
        };
    }

    public static ResolvedTypeface ResolveTypeface(SKTypeface primaryTypeface, string primaryFamily, SKFontStyle fontStyle, string text)
    {
        if (string.IsNullOrEmpty(text) || primaryTypeface.ContainsGlyphs(text))
        {
            return new ResolvedTypeface(primaryTypeface, ownsTypeface: false, FontSimulations.None);
        }

        var firstRune = text.EnumerateRunes().FirstOrDefault();
        if (firstRune.Value == 0)
        {
            return new ResolvedTypeface(primaryTypeface, ownsTypeface: false, FontSimulations.None);
        }

        var fallback = SKFontManager.Default.MatchCharacter(
            primaryFamily,
            fontStyle.Weight,
            fontStyle.Width,
            fontStyle.Slant,
            s_emptyBcp47,
            firstRune.Value);

        return fallback is null
            ? new ResolvedTypeface(primaryTypeface, ownsTypeface: false, FontSimulations.None)
            : new ResolvedTypeface(fallback, ownsTypeface: true, FontSimulations.None);
    }

    public static bool TryCreateTypefaceFromFontManager(Typeface typeface, out SKTypeface skTypeface, out FontSimulations simulations)
    {
        skTypeface = null!;
        simulations = FontSimulations.None;

        try
        {
            if (!FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface) ||
                !glyphTypeface.PlatformTypeface.TryGetStream(out var stream))
            {
                return false;
            }

            using (stream)
            {
                var resolved = SKTypeface.FromStream(stream);

                if (resolved is null)
                {
                    return false;
                }

                skTypeface = resolved;
                simulations = glyphTypeface.PlatformTypeface.FontSimulations;
                return true;
            }
        }
        catch
        {
            skTypeface = null!;
            simulations = FontSimulations.None;
            return false;
        }
    }

    public static IEnumerable<string> EnumerateGraphemes(string text)
    {
        var indexes = StringInfo.ParseCombiningCharacters(text);

        for (var i = 0; i < indexes.Length; i++)
        {
            var start = indexes[i];
            var length = i == indexes.Length - 1 ? text.Length - start : indexes[i + 1] - start;
            yield return text.Substring(start, length);
        }
    }

    private static SKFontStyleSlant ToSkSlant(FontStyle style)
    {
        return style switch
        {
            FontStyle.Italic => SKFontStyleSlant.Italic,
            FontStyle.Oblique => SKFontStyleSlant.Oblique,
            _ => SKFontStyleSlant.Upright,
        };
    }

    internal readonly struct ResolvedTypeface : IDisposable
    {
        private readonly bool _ownsTypeface;

        public ResolvedTypeface(SKTypeface typeface, bool ownsTypeface, FontSimulations simulations)
        {
            Typeface = typeface;
            _ownsTypeface = ownsTypeface;
            Simulations = simulations;
        }

        public SKTypeface Typeface { get; }

        public FontSimulations Simulations { get; }

        public void Dispose()
        {
            if (_ownsTypeface)
            {
                Typeface.Dispose();
            }
        }
    }
}