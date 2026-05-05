using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ProText.Core;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;

namespace ProText.Uno.Internal;

internal static class ProTextUnoAdapter
{
    public static ProTextBrush DefaultForegroundBrush { get; } = new ProTextSolidBrush(ProTextColor.Black, 1);

    public static ProTextBrush DefaultSelectionBrush { get; } = new ProTextSolidBrush(new ProTextColor(96, 0, 120, 215), 1);

    public static ProTextSize ToCore(Size size) => new(size.Width, size.Height);

    public static Size ToUno(ProTextSize size) => new(size.Width, size.Height);

    public static ProTextPoint ToCore(Point point) => new(point.X, point.Y);

    public static Rect ToUno(ProTextRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

    public static ProTextRect ToCore(Rect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

    public static ProTextColor ToCore(Color color) => new(color.A, color.R, color.G, color.B);

    public static ProTextFontStyle ToCore(FontStyle style)
    {
        return style switch
        {
            FontStyle.Italic => ProTextFontStyle.Italic,
            FontStyle.Oblique => ProTextFontStyle.Oblique,
            _ => ProTextFontStyle.Normal,
        };
    }

    public static ProTextWrapping ToCore(TextWrapping wrapping)
    {
        return wrapping switch
        {
            TextWrapping.Wrap => ProTextWrapping.Wrap,
            TextWrapping.WrapWholeWords => ProTextWrapping.Wrap,
            _ => ProTextWrapping.NoWrap,
        };
    }

    public static ProTextTrimming ToCore(TextTrimming trimming)
    {
        return trimming switch
        {
            TextTrimming.WordEllipsis => ProTextTrimming.WordEllipsis,
            TextTrimming.CharacterEllipsis => ProTextTrimming.CharacterEllipsis,
            _ => ProTextTrimming.None,
        };
    }

    public static ProTextTextAlignment ToCore(TextAlignment alignment)
    {
        return alignment switch
        {
            TextAlignment.Center => ProTextTextAlignment.Center,
            TextAlignment.Right => ProTextTextAlignment.Right,
            TextAlignment.Justify => ProTextTextAlignment.Justify,
            TextAlignment.DetectFromContent => ProTextTextAlignment.DetectFromContent,
            _ => ProTextTextAlignment.Left,
        };
    }

    public static ProTextFlowDirection ToCore(FlowDirection flowDirection)
    {
        return flowDirection == FlowDirection.RightToLeft
            ? ProTextFlowDirection.RightToLeft
            : ProTextFlowDirection.LeftToRight;
    }

    public static string GetPrimaryFamilyName(FontFamily? fontFamily)
    {
        var source = fontFamily?.Source;

        return string.IsNullOrWhiteSpace(source)
            ? ProTextFontDescriptor.DefaultFontFamily
            : ProTextFontDescriptor.GetPrimaryFamilyName(source);
    }

    public static string CreateFontFeaturesFingerprint(string? fontFeatures)
    {
        return string.IsNullOrWhiteSpace(fontFeatures)
            ? "none"
            : fontFeatures.Replace(' ', '_');
    }

    public static ProTextBrush? SnapshotBrush(Brush? brush)
    {
        return brush switch
        {
            null => null,
            SolidColorBrush solid => new ProTextSolidBrush(ToCore(solid.Color), solid.Opacity),
            LinearGradientBrush linear => new ProTextLinearGradientBrush(
                SnapshotGradientStops(linear.GradientStops),
                linear.Opacity,
                ToCore(linear.SpreadMethod),
                ToCore(linear.StartPoint, linear.MappingMode),
                ToCore(linear.EndPoint, linear.MappingMode)),
            RadialGradientBrush radial => new ProTextRadialGradientBrush(
                SnapshotGradientStops(radial.GradientStops),
                radial.Opacity,
                ToCore(radial.SpreadMethod),
                ToCore(radial.Center, radial.MappingMode),
                ToCore(radial.RadiusX, radial.MappingMode),
                ToCore(radial.RadiusY, radial.MappingMode)),
            _ => null,
        };
    }

    public static IReadOnlyList<ProTextDecoration> SnapshotDecorations(TextDecorations decorations)
    {
        if (decorations == TextDecorations.None)
        {
            return Array.Empty<ProTextDecoration>();
        }

        var snapshot = new List<ProTextDecoration>(2);

        if ((decorations & TextDecorations.Underline) != 0)
        {
            snapshot.Add(CreateDecoration(ProTextDecorationLocation.Underline));
        }

        if ((decorations & TextDecorations.Strikethrough) != 0)
        {
            snapshot.Add(CreateDecoration(ProTextDecorationLocation.Strikethrough));
        }

        return snapshot;
    }

    public static double NormalizeLineHeight(double lineHeight)
    {
        return lineHeight <= 0 || double.IsNaN(lineHeight) || double.IsInfinity(lineHeight)
            ? double.NaN
            : lineHeight;
    }

    public static double ToLetterSpacing(double fontSize, int characterSpacing, double letterSpacing)
    {
        return letterSpacing + (fontSize * characterSpacing / 1000d);
    }

    public static int GetFontWeight(FontWeight weight) => weight.Weight;

    public static int GetFontStretch(FontStretch stretch) => stretch == FontStretch.Undefined ? (int)FontStretch.Normal : (int)stretch;

    private static ProTextDecoration CreateDecoration(ProTextDecorationLocation location)
    {
        return new ProTextDecoration(
            location,
            null,
            1,
            ProTextDecorationUnit.FontRecommended,
            Array.Empty<double>(),
            0,
            ProTextPenLineCap.Flat,
            0,
            ProTextDecorationUnit.FontRecommended);
    }

    private static IReadOnlyList<ProTextGradientStop> SnapshotGradientStops(IEnumerable<GradientStop> stops)
    {
        return stops.Select(static stop => new ProTextGradientStop(ToCore(stop.Color), stop.Offset)).ToArray();
    }

    private static ProTextRelativePoint ToCore(Point point, BrushMappingMode mappingMode)
    {
        return new ProTextRelativePoint(point.X, point.Y, ToCore(mappingMode));
    }

    private static ProTextRelativeScalar ToCore(double scalar, BrushMappingMode mappingMode)
    {
        return new ProTextRelativeScalar(scalar, ToCore(mappingMode));
    }

    private static ProTextRelativeUnit ToCore(BrushMappingMode mappingMode)
    {
        return mappingMode == BrushMappingMode.Absolute ? ProTextRelativeUnit.Absolute : ProTextRelativeUnit.Relative;
    }

    private static ProTextGradientSpreadMethod ToCore(GradientSpreadMethod spreadMethod)
    {
        return spreadMethod switch
        {
            GradientSpreadMethod.Reflect => ProTextGradientSpreadMethod.Reflect,
            GradientSpreadMethod.Repeat => ProTextGradientSpreadMethod.Repeat,
            _ => ProTextGradientSpreadMethod.Pad,
        };
    }
}

internal sealed class UnoProTextTypefaceResolver : IProTextTypefaceResolver
{
    public static UnoProTextTypefaceResolver Instance { get; } = new();

    public bool TryResolveTypeface(ProTextFontIdentity font, out ProTextResolvedTypeface typeface)
    {
        typeface = default;

        try
        {
            var skStyle = ProText.Core.ProTextFontResolver.CreateFontStyle(font.Weight, font.Stretch, font.Style);
            var resolved = SKTypeface.FromFamilyName(font.Family, skStyle);

            if (resolved is null)
            {
                return false;
            }

            typeface = new ProTextResolvedTypeface(resolved, ProTextFontSimulations.None, OwnsTypeface: true);
            return true;
        }
        catch
        {
            typeface = default;
            return false;
        }
    }
}

internal static class ProTextUnoPlatform
{
    private static readonly object s_configureSync = new();
    private static bool s_configured;

    public static void EnsureConfigured()
    {
        ProText.Core.ProTextFontResolver.SetTypefaceResolver(UnoProTextTypefaceResolver.Instance);

        if (!Volatile.Read(ref s_configured))
        {
            lock (s_configureSync)
            {
                if (!s_configured)
                {
                    Volatile.Write(ref s_configured, true);
                }
            }
        }

        ProTextCoreCache.EnsureConfigured();
    }
}

internal static class ProTextFontResolver
{
    public static ProText.Core.ProTextFontResolver.ResolvedTypeface ResolveTypeface(FontFamily family, FontWeight weight, FontStretch stretch, FontStyle style)
    {
        ProTextUnoPlatform.EnsureConfigured();
        return ProText.Core.ProTextFontResolver.ResolveTypeface(
            ProTextUnoAdapter.GetPrimaryFamilyName(family),
            ProTextUnoAdapter.GetFontWeight(weight),
            ProTextUnoAdapter.GetFontStretch(stretch),
            ProTextUnoAdapter.ToCore(style));
    }
}
