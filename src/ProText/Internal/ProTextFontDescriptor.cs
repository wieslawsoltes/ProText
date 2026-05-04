using System.Globalization;
using System.Text;
using Avalonia.Media;

namespace ProText.Internal;

internal static class ProTextFontDescriptor
{
    public static string Create(FontFamily family, double size, FontStyle style, FontWeight weight)
    {
        return Create(family, size, style, weight, FontStretch.Normal, 0, null);
    }

    public static string Create(
        FontFamily family,
        double size,
        FontStyle style,
        FontWeight weight,
        FontStretch stretch,
        double letterSpacing,
        FontFeatureCollection? fontFeatures)
    {
        var familyName = GetPrimaryFamilyName(family);
        var escapedFamily = familyName.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        var stylePrefix = style switch
        {
            FontStyle.Italic => "italic ",
            FontStyle.Oblique => "oblique ",
            _ => string.Empty
        };
        var weightValue = ((int)weight).ToString(CultureInfo.InvariantCulture);
        var sizeValue = size.ToString("0.###", CultureInfo.InvariantCulture);
        var letterSpacingValue = letterSpacing.ToString("0.###", CultureInfo.InvariantCulture);
        var stretchValue = ((int)stretch).ToString(CultureInfo.InvariantCulture);
        var featuresValue = CreateFontFeaturesFingerprint(fontFeatures);

        return $"ptb-ls={letterSpacingValue} ptb-stretch={stretchValue} ptb-features={featuresValue} {stylePrefix}{weightValue} {sizeValue}px \"{escapedFamily}\"";
    }

    public static string GetPrimaryFamilyName(FontFamily family)
    {
        var name = family.Name;

        return string.Equals(name, FontFamily.DefaultFontFamilyName, StringComparison.Ordinal)
            ? "Inter"
            : name;
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

    public static FontStretch GetFontStretch(string font)
    {
        foreach (var token in font.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.StartsWith("ptb-stretch=", StringComparison.Ordinal) &&
                int.TryParse(token.AsSpan("ptb-stretch=".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) &&
                value >= (int)FontStretch.UltraCondensed &&
                value <= (int)FontStretch.UltraExpanded)
            {
                return (FontStretch)value;
            }
        }

        return FontStretch.Normal;
    }

    public static string CreateFontFeaturesFingerprint(FontFeatureCollection? fontFeatures)
    {
        if (fontFeatures is null || fontFeatures.Count == 0)
        {
            return "none";
        }

        var builder = new StringBuilder();

        foreach (var feature in fontFeatures)
        {
            if (builder.Length > 0)
            {
                builder.Append(',');
            }

            builder.Append(feature);
        }

        return builder.ToString().Replace(' ', '_');
    }
}