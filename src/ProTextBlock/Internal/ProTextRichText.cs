using Avalonia;
using Avalonia.Media;
using Pretext;

namespace ProTextBlock.Internal;

internal sealed class ProTextRichStyle
{
    public ProTextRichStyle(
        FontFamily fontFamily,
        double fontSize,
        FontStyle fontStyle,
        FontWeight fontWeight,
        FontStretch fontStretch,
        ProTextBrush? foreground,
        IReadOnlyList<ProTextDecoration> textDecorations,
        FontFeatureCollection? fontFeatures,
        double letterSpacing)
    {
        FontFamily = fontFamily;
        FontSize = fontSize;
        FontStyle = fontStyle;
        FontWeight = fontWeight;
        FontStretch = fontStretch;
        Foreground = foreground;
        TextDecorations = textDecorations;
        FontFeatures = fontFeatures is { Count: > 0 } ? new FontFeatureCollection(fontFeatures) : null;
        LetterSpacing = letterSpacing;
        FontDescriptor = ProTextBlockFontDescriptor.Create(
            FontFamily,
            FontSize,
            FontStyle,
            FontWeight,
            FontStretch,
            LetterSpacing,
            FontFeatures);
        RenderFingerprint = string.Concat(
            FontDescriptor,
            "|fg=",
            Foreground?.Fingerprint ?? "null",
            "|td=",
            TextDecorations.Count == 0 ? "none" : string.Join(';', TextDecorations.Select(static decoration => decoration.Fingerprint)));
    }

    public FontFamily FontFamily { get; }

    public double FontSize { get; }

    public FontStyle FontStyle { get; }

    public FontWeight FontWeight { get; }

    public FontStretch FontStretch { get; }

    public ProTextBrush? Foreground { get; }

    public IReadOnlyList<ProTextDecoration> TextDecorations { get; }

    public FontFeatureCollection? FontFeatures { get; }

    public double LetterSpacing { get; }

    public string FontDescriptor { get; }

    public string RenderFingerprint { get; }
}

internal sealed record ProTextRichRun(string Text, ProTextRichStyle Style)
{
    public RichInlineItem ToRichInlineItem() => new(Text, Style.FontDescriptor);
}

internal sealed class ProTextRichParagraph
{
    public ProTextRichParagraph(IReadOnlyList<ProTextRichRun> runs, string layoutFingerprint, string renderFingerprint)
    {
        Runs = runs;
        LayoutFingerprint = layoutFingerprint;
        RenderFingerprint = renderFingerprint;
    }

    public IReadOnlyList<ProTextRichRun> Runs { get; }

    public string LayoutFingerprint { get; }

    public string RenderFingerprint { get; }

    public RichInlineItem[] CreateInlineItems()
    {
        var items = new RichInlineItem[Runs.Count];

        for (var i = 0; i < Runs.Count; i++)
        {
            items[i] = Runs[i].ToRichInlineItem();
        }

        return items;
    }
}

internal sealed class ProTextRichContent
{
    public ProTextRichContent(IReadOnlyList<ProTextRichParagraph> paragraphs, string layoutFingerprint, string renderFingerprint, double maxFontSize)
    {
        Paragraphs = paragraphs;
        LayoutFingerprint = layoutFingerprint;
        RenderFingerprint = renderFingerprint;
        MaxFontSize = maxFontSize;
    }

    public IReadOnlyList<ProTextRichParagraph> Paragraphs { get; }

    public string LayoutFingerprint { get; }

    public string RenderFingerprint { get; }

    public double MaxFontSize { get; }
}

internal readonly record struct ProTextRichCacheKey(string Fingerprint);

internal sealed class ProTextPreparedContent
{
    public ProTextPreparedContent(IReadOnlyList<PreparedRichInline> paragraphs)
    {
        Paragraphs = paragraphs;
    }

    public IReadOnlyList<PreparedRichInline> Paragraphs { get; }
}

internal abstract record ProTextBrush(double Opacity)
{
    public abstract string Fingerprint { get; }
}

internal sealed record ProTextSolidBrush(Color Color, double Opacity) : ProTextBrush(Opacity)
{
    public override string Fingerprint { get; } = $"solid:{Color}:{Opacity:0.###}";
}

internal sealed record ProTextLinearGradientBrush(
    IReadOnlyList<ProTextGradientStop> GradientStops,
    double Opacity,
    GradientSpreadMethod SpreadMethod,
    RelativePoint StartPoint,
    RelativePoint EndPoint) : ProTextBrush(Opacity)
{
    public override string Fingerprint { get; } = $"linear:{Opacity:0.###}:{SpreadMethod}:{StartPoint}:{EndPoint}:{string.Join(',', GradientStops)}";
}

internal sealed record ProTextRadialGradientBrush(
    IReadOnlyList<ProTextGradientStop> GradientStops,
    double Opacity,
    GradientSpreadMethod SpreadMethod,
    RelativePoint Center,
    RelativeScalar RadiusX,
    RelativeScalar RadiusY) : ProTextBrush(Opacity)
{
    public override string Fingerprint { get; } = $"radial:{Opacity:0.###}:{SpreadMethod}:{Center}:{RadiusX}:{RadiusY}:{string.Join(',', GradientStops)}";
}

internal sealed record ProTextConicGradientBrush(
    IReadOnlyList<ProTextGradientStop> GradientStops,
    double Opacity,
    GradientSpreadMethod SpreadMethod,
    RelativePoint Center,
    double Angle) : ProTextBrush(Opacity)
{
    public override string Fingerprint { get; } = $"conic:{Opacity:0.###}:{SpreadMethod}:{Center}:{Angle:0.###}:{string.Join(',', GradientStops)}";
}

internal readonly record struct ProTextGradientStop(Color Color, double Offset)
{
    public override string ToString() => $"{Color}@{Offset:0.###}";
}

internal sealed record ProTextDecoration(
    TextDecorationLocation Location,
    ProTextBrush? Stroke,
    double StrokeThickness,
    TextDecorationUnit StrokeThicknessUnit,
    IReadOnlyList<double> StrokeDashArray,
    double StrokeDashOffset,
    PenLineCap StrokeLineCap,
    double StrokeOffset,
    TextDecorationUnit StrokeOffsetUnit)
{
    public string Fingerprint { get; } = string.Concat(
        Location,
        ':',
        Stroke?.Fingerprint ?? "default",
        ':',
        StrokeThickness.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
        ':',
        StrokeThicknessUnit,
        ':',
        string.Join(',', StrokeDashArray.Select(static dash => dash.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))),
        ':',
        StrokeDashOffset.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
        ':',
        StrokeLineCap,
        ':',
        StrokeOffset.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
        ':',
        StrokeOffsetUnit);
}