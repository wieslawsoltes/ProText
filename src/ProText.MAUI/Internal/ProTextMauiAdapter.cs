using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using ProText.Core;
using SkiaSharp;

namespace ProText.MAUI.Internal;

internal static class ProTextMauiAdapter
{
    public static ProTextBrush DefaultForegroundBrush { get; } = new ProTextSolidBrush(ProTextColor.Black, 1);

    public static ProTextBrush DefaultSelectionBrush { get; } = new ProTextSolidBrush(new ProTextColor(96, 0, 120, 215), 1);

    public static ProTextSize ToCore(Size size) => new(size.Width, size.Height);

    public static Size ToMaui(ProTextSize size) => new(size.Width, size.Height);

    public static ProTextPoint ToCore(Point point) => new(point.X, point.Y);

    public static Rect ToMaui(ProTextRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

    public static ProTextRect ToCore(Rect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

    public static ProTextColor ToCore(Color color)
    {
        return new ProTextColor(
            ToByte(color.Alpha),
            ToByte(color.Red),
            ToByte(color.Green),
            ToByte(color.Blue));
    }

    public static ProTextFontStyle ToCore(FontAttributes attributes)
    {
        return (attributes & FontAttributes.Italic) != 0
            ? ProTextFontStyle.Italic
            : ProTextFontStyle.Normal;
    }

    public static ProTextWrapping ToWrapping(LineBreakMode lineBreakMode)
    {
        return lineBreakMode switch
        {
            LineBreakMode.WordWrap => ProTextWrapping.Wrap,
            LineBreakMode.CharacterWrap => ProTextWrapping.Wrap,
            _ => ProTextWrapping.NoWrap,
        };
    }

    public static ProTextTrimming ToTrimming(LineBreakMode lineBreakMode)
    {
        return lineBreakMode switch
        {
            LineBreakMode.HeadTruncation => ProTextTrimming.CharacterEllipsis,
            LineBreakMode.MiddleTruncation => ProTextTrimming.CharacterEllipsis,
            LineBreakMode.TailTruncation => ProTextTrimming.CharacterEllipsis,
            _ => ProTextTrimming.None,
        };
    }

    public static ProTextTextAlignment ToCore(TextAlignment alignment)
    {
        return alignment switch
        {
            TextAlignment.Center => ProTextTextAlignment.Center,
            TextAlignment.End => ProTextTextAlignment.Right,
            _ => ProTextTextAlignment.Left,
        };
    }

    public static ProTextFlowDirection ToCore(FlowDirection flowDirection)
    {
        return flowDirection == FlowDirection.RightToLeft
            ? ProTextFlowDirection.RightToLeft
            : ProTextFlowDirection.LeftToRight;
    }

    public static string GetPrimaryFamilyName(string? fontFamily)
    {
        return string.IsNullOrWhiteSpace(fontFamily)
            ? ProTextFontDescriptor.DefaultFontFamily
            : ProTextFontDescriptor.GetPrimaryFamilyName(fontFamily);
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
            SolidColorBrush solid => new ProTextSolidBrush(ToCore(solid.Color), 1),
            LinearGradientBrush linear => new ProTextLinearGradientBrush(
                SnapshotGradientStops(linear.GradientStops),
                1,
                ProTextGradientSpreadMethod.Pad,
                ToRelativePoint(linear.StartPoint),
                ToRelativePoint(linear.EndPoint)),
            RadialGradientBrush radial => new ProTextRadialGradientBrush(
                SnapshotGradientStops(radial.GradientStops),
                1,
                ProTextGradientSpreadMethod.Pad,
                ToRelativePoint(radial.Center),
                ToRelativeScalar(radial.Radius),
                ToRelativeScalar(radial.Radius)),
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

    public static double ToLetterSpacing(double characterSpacing, double letterSpacing)
    {
        return letterSpacing + characterSpacing;
    }

    public static int GetFontWeight(int fontWeight, FontAttributes attributes)
    {
        var normalized = fontWeight <= 0 ? 400 : fontWeight;
        return (attributes & FontAttributes.Bold) != 0 ? Math.Max(normalized, 700) : normalized;
    }

    public static int GetFontStretch(int fontStretch) => fontStretch <= 0 ? 5 : fontStretch;

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

    private static ProTextRelativePoint ToRelativePoint(Point point)
    {
        return new ProTextRelativePoint(point.X, point.Y, ProTextRelativeUnit.Relative);
    }

    private static ProTextRelativeScalar ToRelativeScalar(double scalar)
    {
        return new ProTextRelativeScalar(scalar, ProTextRelativeUnit.Relative);
    }

    private static byte ToByte(float component)
    {
        return (byte)Math.Clamp(Math.Round(component * byte.MaxValue), 0, byte.MaxValue);
    }

    private static byte ToByte(double component)
    {
        return (byte)Math.Clamp(Math.Round(component * byte.MaxValue), 0, byte.MaxValue);
    }
}

internal sealed class MauiProTextTypefaceResolver : IProTextTypefaceResolver
{
    public static MauiProTextTypefaceResolver Instance { get; } = new();

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

internal static class ProTextMauiPlatform
{
    private static readonly object s_configureSync = new();
    private static bool s_configured;

    public static void EnsureConfigured()
    {
        ProText.Core.ProTextFontResolver.SetTypefaceResolver(MauiProTextTypefaceResolver.Instance);

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
    public static ProText.Core.ProTextFontResolver.ResolvedTypeface ResolveTypeface(string? family, int weight, int stretch, FontAttributes attributes)
    {
        ProTextMauiPlatform.EnsureConfigured();
        return ProText.Core.ProTextFontResolver.ResolveTypeface(
            ProTextMauiAdapter.GetPrimaryFamilyName(family),
            ProTextMauiAdapter.GetFontWeight(weight, attributes),
            ProTextMauiAdapter.GetFontStretch(stretch),
            ProTextMauiAdapter.ToCore(attributes));
    }
}
