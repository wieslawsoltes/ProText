using System.Text;
using Avalonia;
using Avalonia.Media;
using ProText.Core;
using SkiaSharp;

namespace ProText.Avalonia.Internal;

internal static class ProTextAvaloniaAdapter
{
    public static ProTextSize ToCore(Size size) => new(size.Width, size.Height);

    public static Size ToAvalonia(ProTextSize size) => new(size.Width, size.Height);

    public static ProTextPoint ToCore(Point point) => new(point.X, point.Y);

    public static Rect ToAvalonia(ProTextRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

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
            TextWrapping.WrapWithOverflow => ProTextWrapping.WrapWithOverflow,
            _ => ProTextWrapping.NoWrap,
        };
    }

    public static ProTextTrimming ToCore(TextTrimming trimming)
    {
        if (ReferenceEquals(trimming, TextTrimming.None))
        {
            return ProTextTrimming.None;
        }

        if (ReferenceEquals(trimming, TextTrimming.WordEllipsis))
        {
            return ProTextTrimming.WordEllipsis;
        }

        if (ReferenceEquals(trimming, TextTrimming.CharacterEllipsis)
            || ReferenceEquals(trimming, TextTrimming.PrefixCharacterEllipsis)
            || ReferenceEquals(trimming, TextTrimming.LeadingCharacterEllipsis)
            || ReferenceEquals(trimming, TextTrimming.PathSegmentEllipsis))
        {
            return ProTextTrimming.CharacterEllipsis;
        }

        return ProTextTrimming.CharacterEllipsis;
    }

    public static ProTextTextAlignment ToCore(TextAlignment alignment)
    {
        return alignment switch
        {
            TextAlignment.Center => ProTextTextAlignment.Center,
            TextAlignment.Right => ProTextTextAlignment.Right,
            TextAlignment.Justify => ProTextTextAlignment.Justify,
            TextAlignment.Start => ProTextTextAlignment.Start,
            TextAlignment.End => ProTextTextAlignment.End,
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

    public static string GetPrimaryFamilyName(FontFamily fontFamily)
    {
        var name = fontFamily.Name;

        return string.Equals(name, FontFamily.DefaultFontFamilyName, StringComparison.Ordinal)
            ? ProTextFontDescriptor.DefaultFontFamily
            : name;
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

    public static ProTextBrush? SnapshotBrush(IBrush? brush)
    {
        return brush switch
        {
            null => null,
            ISolidColorBrush solid => new ProTextSolidBrush(ToCore(solid.Color), solid.Opacity),
            ILinearGradientBrush linear => new ProTextLinearGradientBrush(SnapshotGradientStops(linear), linear.Opacity, ToCore(linear.SpreadMethod), ToCore(linear.StartPoint), ToCore(linear.EndPoint)),
            IRadialGradientBrush radial => new ProTextRadialGradientBrush(SnapshotGradientStops(radial), radial.Opacity, ToCore(radial.SpreadMethod), ToCore(radial.Center), ToCore(radial.RadiusX), ToCore(radial.RadiusY)),
            IConicGradientBrush conic => new ProTextConicGradientBrush(SnapshotGradientStops(conic), conic.Opacity, ToCore(conic.SpreadMethod), ToCore(conic.Center), conic.Angle),
            _ => null
        };
    }

    public static IReadOnlyList<ProTextDecoration> SnapshotDecorations(TextDecorationCollection? decorations)
    {
        if (decorations is null || decorations.Count == 0)
        {
            return Array.Empty<ProTextDecoration>();
        }

        var snapshot = new ProTextDecoration[decorations.Count];

        for (var i = 0; i < decorations.Count; i++)
        {
            var decoration = decorations[i];
            snapshot[i] = new ProTextDecoration(
                ToCore(decoration.Location),
                SnapshotBrush(decoration.Stroke),
                decoration.StrokeThickness,
                ToCore(decoration.StrokeThicknessUnit),
                decoration.StrokeDashArray?.ToArray() ?? Array.Empty<double>(),
                decoration.StrokeDashOffset,
                ToCore(decoration.StrokeLineCap),
                decoration.StrokeOffset,
                ToCore(decoration.StrokeOffsetUnit));
        }

        return snapshot;
    }

    public static ProTextRelativePoint ToCore(RelativePoint point)
    {
        return new ProTextRelativePoint(point.Point.X, point.Point.Y, ToCore(point.Unit));
    }

    public static ProTextRelativeScalar ToCore(RelativeScalar scalar)
    {
        return new ProTextRelativeScalar(scalar.Scalar, ToCore(scalar.Unit));
    }

    private static IReadOnlyList<ProTextGradientStop> SnapshotGradientStops(IGradientBrush brush)
    {
        var stops = new ProTextGradientStop[brush.GradientStops.Count];

        for (var i = 0; i < stops.Length; i++)
        {
            var stop = brush.GradientStops[i];
            stops[i] = new ProTextGradientStop(ToCore(stop.Color), stop.Offset);
        }

        return stops;
    }

    private static ProTextRelativeUnit ToCore(RelativeUnit unit)
    {
        return unit == RelativeUnit.Absolute ? ProTextRelativeUnit.Absolute : ProTextRelativeUnit.Relative;
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

    private static ProTextDecorationLocation ToCore(TextDecorationLocation location)
    {
        return location switch
        {
            TextDecorationLocation.Overline => ProTextDecorationLocation.Overline,
            TextDecorationLocation.Strikethrough => ProTextDecorationLocation.Strikethrough,
            TextDecorationLocation.Baseline => ProTextDecorationLocation.Baseline,
            _ => ProTextDecorationLocation.Underline,
        };
    }

    private static ProTextDecorationUnit ToCore(TextDecorationUnit unit)
    {
        return unit switch
        {
            TextDecorationUnit.FontRenderingEmSize => ProTextDecorationUnit.FontRenderingEmSize,
            TextDecorationUnit.Pixel => ProTextDecorationUnit.Pixel,
            _ => ProTextDecorationUnit.FontRecommended,
        };
    }

    private static ProTextPenLineCap ToCore(PenLineCap cap)
    {
        return cap switch
        {
            PenLineCap.Round => ProTextPenLineCap.Round,
            PenLineCap.Square => ProTextPenLineCap.Square,
            _ => ProTextPenLineCap.Flat,
        };
    }
}

internal sealed class AvaloniaProTextTypefaceResolver : IProTextTypefaceResolver
{
    public static AvaloniaProTextTypefaceResolver Instance { get; } = new();

    public bool TryResolveTypeface(ProTextFontIdentity font, out ProTextResolvedTypeface typeface)
    {
        typeface = default;

        try
        {
            var avaloniaStyle = font.Style switch
            {
                ProTextFontStyle.Italic => FontStyle.Italic,
                ProTextFontStyle.Oblique => FontStyle.Oblique,
                _ => FontStyle.Normal,
            };
            var avaloniaWeight = (FontWeight)font.Weight;
            var avaloniaStretch = (FontStretch)font.Stretch;
            var avaloniaTypeface = new Typeface(new FontFamily(font.Family), avaloniaStyle, avaloniaWeight, avaloniaStretch);

            if (!FontManager.Current.TryGetGlyphTypeface(avaloniaTypeface, out var glyphTypeface) ||
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

                typeface = new ProTextResolvedTypeface(
                    resolved,
                    ToCore(glyphTypeface.PlatformTypeface.FontSimulations),
                    OwnsTypeface: true);
                return true;
            }
        }
        catch
        {
            typeface = default;
            return false;
        }
    }

    private static ProTextFontSimulations ToCore(FontSimulations simulations)
    {
        var result = ProTextFontSimulations.None;

        if ((simulations & FontSimulations.Bold) != 0)
        {
            result |= ProTextFontSimulations.Bold;
        }

        if ((simulations & FontSimulations.Oblique) != 0)
        {
            result |= ProTextFontSimulations.Oblique;
        }

        return result;
    }
}

internal static class ProTextAvaloniaPlatform
{
    private static readonly object s_configureSync = new();
    private static bool s_configured;

    public static void EnsureConfigured()
    {
        ProText.Core.ProTextFontResolver.SetTypefaceResolver(AvaloniaProTextTypefaceResolver.Instance);

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
        ProTextAvaloniaPlatform.EnsureConfigured();
        return ProText.Core.ProTextFontResolver.ResolveTypeface(
            ProTextAvaloniaAdapter.GetPrimaryFamilyName(family),
            (int)weight,
            (int)stretch,
            ProTextAvaloniaAdapter.ToCore(style));
    }
}
