using System.Globalization;

namespace ProText.Core;

/// <summary>
/// Creates stable font descriptor strings used by PretextSharp and ProText cache keys.
/// </summary>
public static class ProTextFontDescriptor
{
    public const string DefaultFontFamily = "Inter";

    public static string Create(string family, double size, ProTextFontStyle style, int weight)
    {
        return Create(family, size, style, weight, stretch: 5, letterSpacing: 0, fontFeaturesFingerprint: null);
    }

    public static string Create(
        string family,
        double size,
        ProTextFontStyle style,
        int weight,
        int stretch,
        double letterSpacing,
        string? fontFeaturesFingerprint)
    {
        var familyName = GetPrimaryFamilyName(family);
        var escapedFamily = familyName.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        var stylePrefix = style switch
        {
            ProTextFontStyle.Italic => "italic ",
            ProTextFontStyle.Oblique => "oblique ",
            _ => string.Empty
        };
        var weightValue = weight.ToString(CultureInfo.InvariantCulture);
        var sizeValue = size.ToString("0.###", CultureInfo.InvariantCulture);
        var letterSpacingValue = letterSpacing.ToString("0.###", CultureInfo.InvariantCulture);
        var stretchValue = stretch.ToString(CultureInfo.InvariantCulture);
        var featuresValue = string.IsNullOrWhiteSpace(fontFeaturesFingerprint) ? "none" : fontFeaturesFingerprint;

        return $"ptb-ls={letterSpacingValue} ptb-stretch={stretchValue} ptb-features={featuresValue} {stylePrefix}{weightValue} {sizeValue}px \"{escapedFamily}\"";
    }

    public static string GetPrimaryFamilyName(string? family)
    {
        if (string.IsNullOrWhiteSpace(family) || string.Equals(family, "Default", StringComparison.Ordinal))
        {
            return DefaultFontFamily;
        }

        return family;
    }

    public static double GetLetterSpacing(string font)
    {
        foreach (var token in font.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.StartsWith("ptb-ls=", StringComparison.Ordinal) &&
                double.TryParse(token.AsSpan("ptb-ls=".Length), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
        }

        return 0;
    }

    public static int GetFontStretch(string font)
    {
        foreach (var token in font.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.StartsWith("ptb-stretch=", StringComparison.Ordinal) &&
                int.TryParse(token.AsSpan("ptb-stretch=".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
        }

        return 5;
    }
}
