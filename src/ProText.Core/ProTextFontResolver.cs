using System.Globalization;
using SkiaSharp;

namespace ProText.Core;

/// <summary>
/// Provides host-specific primary typefaces for ProText's Skia text path.
/// </summary>
public interface IProTextTypefaceResolver
{
    bool TryResolveTypeface(ProTextFontIdentity font, out ProTextResolvedTypeface typeface);
}

/// <summary>
/// Framework-neutral font identity used by core font resolution.
/// </summary>
public readonly record struct ProTextFontIdentity(
    string Family,
    int Weight,
    int Stretch,
    ProTextFontStyle Style);

/// <summary>
/// A typeface resolved by a host adapter and its ownership contract.
/// </summary>
public readonly record struct ProTextResolvedTypeface(
    SKTypeface Typeface,
    ProTextFontSimulations Simulations,
    bool OwnsTypeface);

/// <summary>
/// Resolves Skia typefaces and fallback fonts for measurement and rendering.
/// </summary>
public static class ProTextFontResolver
{
    private static readonly string[] s_emptyBcp47 = [];
    private static IProTextTypefaceResolver s_typefaceResolver = DefaultTypefaceResolver.Instance;

    public static void SetTypefaceResolver(IProTextTypefaceResolver? resolver)
    {
        Volatile.Write(ref s_typefaceResolver, resolver ?? DefaultTypefaceResolver.Instance);
    }

    public static SKFontStyle CreateFontStyle(int weight, int stretch, ProTextFontStyle style)
    {
        return new SKFontStyle(weight, stretch, ToSkSlant(style));
    }

    public static SKFontStyle CreateFontStyle(int weight, int stretch, bool italic)
    {
        return new SKFontStyle(weight, stretch, italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
    }

    public static SKTypeface CreateTypeface(string family, SKFontStyle style)
    {
        return SKTypeface.FromFamilyName(family, style) ?? SKTypeface.CreateDefault();
    }

    public static ResolvedTypeface ResolveTypeface(string family, int weight, int stretch, ProTextFontStyle style)
    {
        var identity = new ProTextFontIdentity(ProTextFontDescriptor.GetPrimaryFamilyName(family), weight, stretch, style);

        if (Volatile.Read(ref s_typefaceResolver).TryResolveTypeface(identity, out var resolvedTypeface))
        {
            return new ResolvedTypeface(resolvedTypeface.Typeface, resolvedTypeface.OwnsTypeface, resolvedTypeface.Simulations);
        }

        var skStyle = CreateFontStyle(identity.Weight, identity.Stretch, identity.Style);
        return new ResolvedTypeface(CreateTypeface(identity.Family, skStyle), ownsTypeface: true, ProTextFontSimulations.None);
    }

    public static SKFont CreateFont(SKTypeface typeface, double fontSize, ProTextFontSimulations simulations = ProTextFontSimulations.None)
    {
        return new SKFont(typeface, (float)fontSize, skewX: (simulations & ProTextFontSimulations.Oblique) != 0 ? -0.3f : 0f)
        {
            Subpixel = true,
            LinearMetrics = true,
            Embolden = (simulations & ProTextFontSimulations.Bold) != 0,
        };
    }

    public static ResolvedTypeface ResolveTypeface(SKTypeface primaryTypeface, string primaryFamily, SKFontStyle fontStyle, string text)
    {
        if (string.IsNullOrEmpty(text) || primaryTypeface.ContainsGlyphs(text))
        {
            return new ResolvedTypeface(primaryTypeface, ownsTypeface: false, ProTextFontSimulations.None);
        }

        var firstRune = text.EnumerateRunes().FirstOrDefault();
        if (firstRune.Value == 0)
        {
            return new ResolvedTypeface(primaryTypeface, ownsTypeface: false, ProTextFontSimulations.None);
        }

        var fallback = SKFontManager.Default.MatchCharacter(
            primaryFamily,
            fontStyle.Weight,
            fontStyle.Width,
            fontStyle.Slant,
            s_emptyBcp47,
            firstRune.Value);

        return fallback is null
            ? new ResolvedTypeface(primaryTypeface, ownsTypeface: false, ProTextFontSimulations.None)
            : new ResolvedTypeface(fallback, ownsTypeface: true, ProTextFontSimulations.None);
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

    private static SKFontStyleSlant ToSkSlant(ProTextFontStyle style)
    {
        return style switch
        {
            ProTextFontStyle.Italic => SKFontStyleSlant.Italic,
            ProTextFontStyle.Oblique => SKFontStyleSlant.Oblique,
            _ => SKFontStyleSlant.Upright,
        };
    }

    public readonly struct ResolvedTypeface : IDisposable
    {
        public ResolvedTypeface(SKTypeface typeface, bool ownsTypeface, ProTextFontSimulations simulations)
        {
            Typeface = typeface;
            OwnsTypeface = ownsTypeface;
            Simulations = simulations;
        }

        public SKTypeface Typeface { get; }

        public bool OwnsTypeface { get; }

        public ProTextFontSimulations Simulations { get; }

        public void Dispose()
        {
            if (OwnsTypeface)
            {
                Typeface.Dispose();
            }
        }
    }

    private sealed class DefaultTypefaceResolver : IProTextTypefaceResolver
    {
        public static DefaultTypefaceResolver Instance { get; } = new();

        public bool TryResolveTypeface(ProTextFontIdentity font, out ProTextResolvedTypeface typeface)
        {
            typeface = new ProTextResolvedTypeface(
                CreateTypeface(font.Family, CreateFontStyle(font.Weight, font.Stretch, font.Style)),
                ProTextFontSimulations.None,
                OwnsTypeface: true);
            return true;
        }
    }
}
