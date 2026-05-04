using Avalonia.Controls.Documents;
using Avalonia.Media;
using ProText.Core;

namespace ProText.Avalonia.Internal;

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
            ProTextAvaloniaAdapter.GetPrimaryFamilyName(fontFamily),
            fontSize,
            ProTextAvaloniaAdapter.ToCore(fontStyle),
            (int)fontWeight,
            (int)fontStretch,
            ProTextAvaloniaAdapter.SnapshotBrush(foreground),
            ProTextAvaloniaAdapter.SnapshotDecorations(textDecorations),
            ProTextAvaloniaAdapter.CreateFontFeaturesFingerprint(fontFeatures),
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
            inline.IsSet(TextElement.FontFamilyProperty) ? ProTextAvaloniaAdapter.GetPrimaryFamilyName(inline.FontFamily) : parent.FontFamily,
            inline.IsSet(TextElement.FontSizeProperty) ? inline.FontSize : parent.FontSize,
            inline.IsSet(TextElement.FontStyleProperty) ? ProTextAvaloniaAdapter.ToCore(inline.FontStyle) : parent.FontStyle,
            inline.IsSet(TextElement.FontWeightProperty) ? (int)inline.FontWeight : parent.FontWeight,
            inline.IsSet(TextElement.FontStretchProperty) ? (int)inline.FontStretch : parent.FontStretch,
            inline.IsSet(TextElement.ForegroundProperty) ? ProTextAvaloniaAdapter.SnapshotBrush(inline.Foreground) : parent.Foreground,
            inline.IsSet(Inline.TextDecorationsProperty) ? ProTextAvaloniaAdapter.SnapshotDecorations(inline.TextDecorations) : parent.TextDecorations,
            inline.IsSet(TextElement.FontFeaturesProperty) ? ProTextAvaloniaAdapter.CreateFontFeaturesFingerprint(inline.FontFeatures) : parent.FontFeaturesFingerprint,
            inline.IsSet(TextElement.LetterSpacingProperty) ? inline.LetterSpacing : parent.LetterSpacing);
    }
}
