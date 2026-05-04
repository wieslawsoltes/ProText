using Pretext;

namespace ProText.Core;

/// <summary>
/// Immutable render and layout style for one ProText rich run.
/// </summary>
public sealed class ProTextRichStyle
{
    public ProTextRichStyle(
        string fontFamily,
        double fontSize,
        ProTextFontStyle fontStyle,
        int fontWeight,
        int fontStretch,
        ProTextBrush? foreground,
        IReadOnlyList<ProTextDecoration> textDecorations,
        string? fontFeaturesFingerprint,
        double letterSpacing)
    {
        FontFamily = string.IsNullOrWhiteSpace(fontFamily) ? ProTextFontDescriptor.DefaultFontFamily : fontFamily;
        FontSize = fontSize;
        FontStyle = fontStyle;
        FontWeight = fontWeight;
        FontStretch = fontStretch;
        Foreground = foreground;
        TextDecorations = textDecorations.Count == 0 ? Array.Empty<ProTextDecoration>() : textDecorations.ToArray();
        FontFeaturesFingerprint = string.IsNullOrWhiteSpace(fontFeaturesFingerprint) ? "none" : fontFeaturesFingerprint;
        LetterSpacing = letterSpacing;
        FontDescriptor = ProTextFontDescriptor.Create(
            FontFamily,
            FontSize,
            FontStyle,
            FontWeight,
            FontStretch,
            LetterSpacing,
            FontFeaturesFingerprint);
        RenderFingerprint = string.Concat(
            FontDescriptor,
            "|fg=",
            Foreground?.Fingerprint ?? "null",
            "|td=",
            TextDecorations.Count == 0 ? "none" : string.Join(';', TextDecorations.Select(static decoration => decoration.Fingerprint)));
    }

    public string FontFamily { get; }

    public double FontSize { get; }

    public ProTextFontStyle FontStyle { get; }

    public int FontWeight { get; }

    public int FontStretch { get; }

    public ProTextBrush? Foreground { get; }

    public IReadOnlyList<ProTextDecoration> TextDecorations { get; }

    public string FontFeaturesFingerprint { get; }

    public double LetterSpacing { get; }

    public string FontDescriptor { get; }

    public string RenderFingerprint { get; }
}

public sealed record ProTextRichRun(string Text, ProTextRichStyle Style, int TextStart)
{
    public RichInlineItem ToRichInlineItem() => new(Text, Style.FontDescriptor);
}

public sealed class ProTextRichParagraph
{
    public ProTextRichParagraph(IReadOnlyList<ProTextRichRun> runs, string layoutFingerprint, string renderFingerprint)
    {
        Runs = runs.Count == 0 ? Array.Empty<ProTextRichRun>() : runs.ToArray();
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

public sealed class ProTextRichContent
{
    public ProTextRichContent(IReadOnlyList<ProTextRichParagraph> paragraphs, string layoutFingerprint, string renderFingerprint, double maxFontSize, string text)
    {
        Paragraphs = paragraphs.Count == 0 ? Array.Empty<ProTextRichParagraph>() : paragraphs.ToArray();
        LayoutFingerprint = layoutFingerprint;
        RenderFingerprint = renderFingerprint;
        MaxFontSize = maxFontSize;
        Text = text;
    }

    public IReadOnlyList<ProTextRichParagraph> Paragraphs { get; }

    public string LayoutFingerprint { get; }

    public string RenderFingerprint { get; }

    public double MaxFontSize { get; }

    public string Text { get; }
}

public readonly record struct ProTextRichCacheKey(string Fingerprint);

public sealed class ProTextPreparedContent
{
    public ProTextPreparedContent(IReadOnlyList<PreparedRichInline> paragraphs)
    {
        Paragraphs = paragraphs.Count == 0 ? Array.Empty<PreparedRichInline>() : paragraphs.ToArray();
    }

    public IReadOnlyList<PreparedRichInline> Paragraphs { get; }
}

public abstract record ProTextBrush(double Opacity)
{
    public abstract string Fingerprint { get; }
}

public sealed record ProTextSolidBrush(ProTextColor Color, double Opacity) : ProTextBrush(Opacity)
{
    public override string Fingerprint { get; } = $"solid:{Color}:{Opacity:0.###}";
}

public sealed record ProTextLinearGradientBrush : ProTextBrush
{
    public ProTextLinearGradientBrush(
        IReadOnlyList<ProTextGradientStop> gradientStops,
        double opacity,
        ProTextGradientSpreadMethod spreadMethod,
        ProTextRelativePoint startPoint,
        ProTextRelativePoint endPoint) : base(opacity)
    {
        GradientStops = gradientStops.Count == 0 ? Array.Empty<ProTextGradientStop>() : gradientStops.ToArray();
        SpreadMethod = spreadMethod;
        StartPoint = startPoint;
        EndPoint = endPoint;
        Fingerprint = $"linear:{Opacity:0.###}:{SpreadMethod}:{StartPoint}:{EndPoint}:{string.Join(',', GradientStops)}";
    }

    public IReadOnlyList<ProTextGradientStop> GradientStops { get; }

    public ProTextGradientSpreadMethod SpreadMethod { get; }

    public ProTextRelativePoint StartPoint { get; }

    public ProTextRelativePoint EndPoint { get; }

    public override string Fingerprint { get; }
}

public sealed record ProTextRadialGradientBrush : ProTextBrush
{
    public ProTextRadialGradientBrush(
        IReadOnlyList<ProTextGradientStop> gradientStops,
        double opacity,
        ProTextGradientSpreadMethod spreadMethod,
        ProTextRelativePoint center,
        ProTextRelativeScalar radiusX,
        ProTextRelativeScalar radiusY) : base(opacity)
    {
        GradientStops = gradientStops.Count == 0 ? Array.Empty<ProTextGradientStop>() : gradientStops.ToArray();
        SpreadMethod = spreadMethod;
        Center = center;
        RadiusX = radiusX;
        RadiusY = radiusY;
        Fingerprint = $"radial:{Opacity:0.###}:{SpreadMethod}:{Center}:{RadiusX}:{RadiusY}:{string.Join(',', GradientStops)}";
    }

    public IReadOnlyList<ProTextGradientStop> GradientStops { get; }

    public ProTextGradientSpreadMethod SpreadMethod { get; }

    public ProTextRelativePoint Center { get; }

    public ProTextRelativeScalar RadiusX { get; }

    public ProTextRelativeScalar RadiusY { get; }

    public override string Fingerprint { get; }
}

public sealed record ProTextConicGradientBrush : ProTextBrush
{
    public ProTextConicGradientBrush(
        IReadOnlyList<ProTextGradientStop> gradientStops,
        double opacity,
        ProTextGradientSpreadMethod spreadMethod,
        ProTextRelativePoint center,
        double angle) : base(opacity)
    {
        GradientStops = gradientStops.Count == 0 ? Array.Empty<ProTextGradientStop>() : gradientStops.ToArray();
        SpreadMethod = spreadMethod;
        Center = center;
        Angle = angle;
        Fingerprint = $"conic:{Opacity:0.###}:{SpreadMethod}:{Center}:{Angle:0.###}:{string.Join(',', GradientStops)}";
    }

    public IReadOnlyList<ProTextGradientStop> GradientStops { get; }

    public ProTextGradientSpreadMethod SpreadMethod { get; }

    public ProTextRelativePoint Center { get; }

    public double Angle { get; }

    public override string Fingerprint { get; }
}

public readonly record struct ProTextGradientStop(ProTextColor Color, double Offset)
{
    public override string ToString() => $"{Color}@{Offset:0.###}";
}

public sealed record ProTextDecoration
{
    public ProTextDecoration(
        ProTextDecorationLocation location,
        ProTextBrush? stroke,
        double strokeThickness,
        ProTextDecorationUnit strokeThicknessUnit,
        IReadOnlyList<double> strokeDashArray,
        double strokeDashOffset,
        ProTextPenLineCap strokeLineCap,
        double strokeOffset,
        ProTextDecorationUnit strokeOffsetUnit)
    {
        Location = location;
        Stroke = stroke;
        StrokeThickness = strokeThickness;
        StrokeThicknessUnit = strokeThicknessUnit;
        StrokeDashArray = strokeDashArray.Count == 0 ? Array.Empty<double>() : strokeDashArray.ToArray();
        StrokeDashOffset = strokeDashOffset;
        StrokeLineCap = strokeLineCap;
        StrokeOffset = strokeOffset;
        StrokeOffsetUnit = strokeOffsetUnit;
        Fingerprint = string.Concat(
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

    public ProTextDecorationLocation Location { get; }

    public ProTextBrush? Stroke { get; }

    public double StrokeThickness { get; }

    public ProTextDecorationUnit StrokeThicknessUnit { get; }

    public IReadOnlyList<double> StrokeDashArray { get; }

    public double StrokeDashOffset { get; }

    public ProTextPenLineCap StrokeLineCap { get; }

    public double StrokeOffset { get; }

    public ProTextDecorationUnit StrokeOffsetUnit { get; }

    public string Fingerprint { get; }
}
