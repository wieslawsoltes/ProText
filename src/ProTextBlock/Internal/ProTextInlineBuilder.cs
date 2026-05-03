using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace ProTextBlock.Internal;

internal static class ProTextInlineBuilder
{
    public static ProTextRichStyle CreateStyle(
        FontFamily fontFamily,
        double fontSize,
        FontStyle fontStyle,
        FontWeight fontWeight,
        FontStretch fontStretch,
        IBrush? foreground,
        TextDecorationCollection? textDecorations,
        FontFeatureCollection? fontFeatures,
        double letterSpacing)
    {
        return new ProTextRichStyle(
            fontFamily,
            fontSize,
            fontStyle,
            fontWeight,
            fontStretch,
            SnapshotBrush(foreground),
            SnapshotDecorations(textDecorations),
            fontFeatures,
            letterSpacing);
    }

    public static ProTextRichContent CreateTextContent(string? text, ProTextRichStyle baseStyle)
    {
        var builder = new ProTextRichContentBuilder(baseStyle);
        builder.AppendText(text ?? string.Empty, baseStyle);
        return builder.Build();
    }

    public static bool TryCreateInlineContent(InlineCollection inlines, ProTextRichStyle baseStyle, out ProTextRichContent content)
    {
        var builder = new ProTextRichContentBuilder(baseStyle);

        foreach (var inline in inlines)
        {
            if (!AppendInline(builder, inline, baseStyle))
            {
                content = null!;
                return false;
            }
        }

        content = builder.Build();
        return true;
    }

    public static bool AppendInline(ProTextRichContentBuilder builder, Inline inline, ProTextRichStyle parentStyle)
    {
        if (inline is InlineUIContainer)
        {
            return true;
        }

        var style = ApplyInlineStyle(inline, parentStyle);

        switch (inline)
        {
            case Run run:
                builder.AppendText(run.Text ?? string.Empty, style);
                return true;
            case LineBreak:
                builder.AppendLineBreak();
                return true;
            case Span span:
                foreach (var child in span.Inlines)
                {
                    if (!AppendInline(builder, child, style))
                    {
                        return false;
                    }
                }

                return true;
            default:
                return false;
        }
    }

    public static ProTextRichStyle ApplyInlineStyle(Inline inline, ProTextRichStyle parent)
    {
        return new ProTextRichStyle(
            inline.IsSet(TextElement.FontFamilyProperty) ? inline.FontFamily : parent.FontFamily,
            inline.IsSet(TextElement.FontSizeProperty) ? inline.FontSize : parent.FontSize,
            inline.IsSet(TextElement.FontStyleProperty) ? inline.FontStyle : parent.FontStyle,
            inline.IsSet(TextElement.FontWeightProperty) ? inline.FontWeight : parent.FontWeight,
            inline.IsSet(TextElement.FontStretchProperty) ? inline.FontStretch : parent.FontStretch,
            inline.IsSet(TextElement.ForegroundProperty) ? SnapshotBrush(inline.Foreground) : parent.Foreground,
            inline.IsSet(Inline.TextDecorationsProperty) ? SnapshotDecorations(inline.TextDecorations) : parent.TextDecorations,
            inline.IsSet(TextElement.FontFeaturesProperty) ? inline.FontFeatures : parent.FontFeatures,
            inline.IsSet(TextElement.LetterSpacingProperty) ? inline.LetterSpacing : parent.LetterSpacing);
    }

    public static ProTextBrush? SnapshotBrush(IBrush? brush)
    {
        return brush switch
        {
            null => null,
            ISolidColorBrush solid => new ProTextSolidBrush(solid.Color, solid.Opacity),
            ILinearGradientBrush linear => new ProTextLinearGradientBrush(SnapshotGradientStops(linear), linear.Opacity, linear.SpreadMethod, linear.StartPoint, linear.EndPoint),
            IRadialGradientBrush radial => new ProTextRadialGradientBrush(SnapshotGradientStops(radial), radial.Opacity, radial.SpreadMethod, radial.Center, radial.RadiusX, radial.RadiusY),
            IConicGradientBrush conic => new ProTextConicGradientBrush(SnapshotGradientStops(conic), conic.Opacity, conic.SpreadMethod, conic.Center, conic.Angle),
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
                decoration.Location,
                SnapshotBrush(decoration.Stroke),
                decoration.StrokeThickness,
                decoration.StrokeThicknessUnit,
                decoration.StrokeDashArray?.ToArray() ?? Array.Empty<double>(),
                decoration.StrokeDashOffset,
                decoration.StrokeLineCap,
                decoration.StrokeOffset,
                decoration.StrokeOffsetUnit);
        }

        return snapshot;
    }

    private static IReadOnlyList<ProTextGradientStop> SnapshotGradientStops(IGradientBrush brush)
    {
        var stops = new ProTextGradientStop[brush.GradientStops.Count];

        for (var i = 0; i < stops.Length; i++)
        {
            var stop = brush.GradientStops[i];
            stops[i] = new ProTextGradientStop(stop.Color, stop.Offset);
        }

        return stops;
    }
}