using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using ProText.Core;
using Windows.UI.Text;

namespace ProText.Uno.Internal;

internal static class ProTextUnoInlineBuilder
{
    public static ProTextRichStyle CreateStyle(
        FontFamily fontFamily,
        double fontSize,
        FontStyle fontStyle,
        FontWeight fontWeight,
        FontStretch fontStretch,
        Brush? foreground,
        TextDecorations textDecorations,
        string? fontFeatures,
        int characterSpacing,
        double letterSpacing)
    {
        return new ProTextRichStyle(
            ProTextUnoAdapter.GetPrimaryFamilyName(fontFamily),
            fontSize,
            ProTextUnoAdapter.ToCore(fontStyle),
            ProTextUnoAdapter.GetFontWeight(fontWeight),
            ProTextUnoAdapter.GetFontStretch(fontStretch),
            ProTextUnoAdapter.SnapshotBrush(foreground),
            ProTextUnoAdapter.SnapshotDecorations(textDecorations),
            ProTextUnoAdapter.CreateFontFeaturesFingerprint(fontFeatures),
            ProTextUnoAdapter.ToLetterSpacing(fontSize, characterSpacing, letterSpacing));
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
        var fontFamily = IsSet(inline, TextElement.FontFamilyProperty)
            ? ProTextUnoAdapter.GetPrimaryFamilyName(inline.FontFamily)
            : parent.FontFamily;
        var fontSize = IsSet(inline, TextElement.FontSizeProperty) ? inline.FontSize : parent.FontSize;
        var fontStyle = IsSet(inline, TextElement.FontStyleProperty) ? ProTextUnoAdapter.ToCore(inline.FontStyle) : parent.FontStyle;
        var fontWeight = IsSet(inline, TextElement.FontWeightProperty) ? ProTextUnoAdapter.GetFontWeight(inline.FontWeight) : parent.FontWeight;
        var fontStretch = IsSet(inline, TextElement.FontStretchProperty) ? ProTextUnoAdapter.GetFontStretch(inline.FontStretch) : parent.FontStretch;
        var foreground = IsSet(inline, TextElement.ForegroundProperty) ? ProTextUnoAdapter.SnapshotBrush(inline.Foreground) : parent.Foreground;
        var decorations = IsSet(inline, TextElement.TextDecorationsProperty)
            ? ProTextUnoAdapter.SnapshotDecorations(inline.TextDecorations)
            : parent.TextDecorations;
        var letterSpacing = IsSet(inline, TextElement.CharacterSpacingProperty)
            ? ProTextUnoAdapter.ToLetterSpacing(fontSize, inline.CharacterSpacing, 0)
            : parent.LetterSpacing;

        if (inline is Bold && !IsSet(inline, TextElement.FontWeightProperty))
        {
            fontWeight = 700;
        }

        if (inline is Italic && !IsSet(inline, TextElement.FontStyleProperty))
        {
            fontStyle = ProTextFontStyle.Italic;
        }

        if (inline is Underline && !decorations.Any(static decoration => decoration.Location == ProTextDecorationLocation.Underline))
        {
            decorations = decorations
                .Concat(ProTextUnoAdapter.SnapshotDecorations(TextDecorations.Underline))
                .ToArray();
        }

        return new ProTextRichStyle(
            fontFamily,
            fontSize,
            fontStyle,
            fontWeight,
            fontStretch,
            foreground,
            decorations,
            parent.FontFeaturesFingerprint,
            letterSpacing);
    }

    private static bool IsSet(DependencyObject dependencyObject, DependencyProperty property)
    {
        return dependencyObject.ReadLocalValue(property) != DependencyProperty.UnsetValue;
    }
}
