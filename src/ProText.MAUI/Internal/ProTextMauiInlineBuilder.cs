using Microsoft.Maui;
using Microsoft.Maui.Controls;
using ProText.Core;

namespace ProText.MAUI.Internal;

internal static class ProTextMauiInlineBuilder
{
    public static ProTextRichStyle CreateStyle(
        string? fontFamily,
        double fontSize,
        FontAttributes fontAttributes,
        int fontWeight,
        int fontStretch,
        Brush? foreground,
        TextDecorations textDecorations,
        string? fontFeatures,
        double characterSpacing,
        double letterSpacing)
    {
        return new ProTextRichStyle(
            ProTextMauiAdapter.GetPrimaryFamilyName(fontFamily),
            fontSize,
            ProTextMauiAdapter.ToCore(fontAttributes),
            ProTextMauiAdapter.GetFontWeight(fontWeight, fontAttributes),
            ProTextMauiAdapter.GetFontStretch(fontStretch),
            ProTextMauiAdapter.SnapshotBrush(foreground),
            ProTextMauiAdapter.SnapshotDecorations(textDecorations),
            ProTextMauiAdapter.CreateFontFeaturesFingerprint(fontFeatures),
            ProTextMauiAdapter.ToLetterSpacing(characterSpacing, letterSpacing));
    }

    public static ProTextRichContent CreateTextContent(string? text, ProTextRichStyle baseStyle)
    {
        var builder = new ProTextRichContentBuilder(baseStyle);
        builder.AppendText(text ?? string.Empty, baseStyle);
        return builder.Build();
    }

    public static bool TryCreateFormattedContent(FormattedString formattedText, ProTextRichStyle baseStyle, out ProTextRichContent content)
    {
        var builder = new ProTextRichContentBuilder(baseStyle);

        foreach (var span in formattedText.Spans)
        {
            AppendSpan(builder, span, baseStyle);
        }

        content = builder.Build();
        return true;
    }

    private static void AppendSpan(ProTextRichContentBuilder builder, Span span, ProTextRichStyle parentStyle)
    {
        var style = ApplySpanStyle(span, parentStyle);
        builder.AppendText(span.Text ?? string.Empty, style);
    }

    private static ProTextRichStyle ApplySpanStyle(Span span, ProTextRichStyle parent)
    {
        var fontFamily = span.IsSet(Span.FontFamilyProperty)
            ? ProTextMauiAdapter.GetPrimaryFamilyName(span.FontFamily)
            : parent.FontFamily;
        var fontSize = span.IsSet(Span.FontSizeProperty) && span.FontSize > 0 ? span.FontSize : parent.FontSize;
        var fontAttributes = span.IsSet(Span.FontAttributesProperty) ? span.FontAttributes : FontAttributes.None;
        var fontStyle = span.IsSet(Span.FontAttributesProperty) ? ProTextMauiAdapter.ToCore(fontAttributes) : parent.FontStyle;
        var fontWeight = span.IsSet(Span.FontAttributesProperty) ? ProTextMauiAdapter.GetFontWeight(400, fontAttributes) : parent.FontWeight;
        var foreground = span.IsSet(Span.TextColorProperty)
            ? new ProTextSolidBrush(ProTextMauiAdapter.ToCore(span.TextColor), 1)
            : parent.Foreground;
        var decorations = span.IsSet(Span.TextDecorationsProperty)
            ? ProTextMauiAdapter.SnapshotDecorations(span.TextDecorations)
            : parent.TextDecorations;
        var letterSpacing = span.IsSet(Span.CharacterSpacingProperty)
            ? ProTextMauiAdapter.ToLetterSpacing(span.CharacterSpacing, 0)
            : parent.LetterSpacing;

        return new ProTextRichStyle(
            fontFamily,
            fontSize,
            fontStyle,
            fontWeight,
            parent.FontStretch,
            foreground,
            decorations,
            parent.FontFeaturesFingerprint,
            letterSpacing);
    }
}
