using System.Reflection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using ProText.Core;

namespace ProText.MAUI.Tests;

public class ProTextMauiInlineBuilderTests
{
    [Fact]
    public void ExplicitSpanFontAttributesClearInheritedBoldWeight()
    {
        var formattedText = new FormattedString();
        formattedText.Spans.Add(new Span
        {
            Text = "normal",
            FontAttributes = FontAttributes.None,
        });
        var baseStyle = CreateStyle(ProTextFontStyle.Normal, fontWeight: 700);

        var content = CreateFormattedContent(formattedText, baseStyle);
        var run = content.Paragraphs.Single().Runs.Single();

        Assert.Equal(400, run.Style.FontWeight);
        Assert.Equal(ProTextFontStyle.Normal, run.Style.FontStyle);
    }

    [Fact]
    public void ExplicitSpanItalicDoesNotKeepInheritedBoldWeight()
    {
        var formattedText = new FormattedString();
        formattedText.Spans.Add(new Span
        {
            Text = "italic",
            FontAttributes = FontAttributes.Italic,
        });
        var baseStyle = CreateStyle(ProTextFontStyle.Normal, fontWeight: 700);

        var content = CreateFormattedContent(formattedText, baseStyle);
        var run = content.Paragraphs.Single().Runs.Single();

        Assert.Equal(400, run.Style.FontWeight);
        Assert.Equal(ProTextFontStyle.Italic, run.Style.FontStyle);
    }

    private static ProTextRichContent CreateFormattedContent(FormattedString formattedText, ProTextRichStyle baseStyle)
    {
        var inlineBuilder = ProTextMauiTestAssembly.RequiredType("ProText.MAUI.Internal.ProTextMauiInlineBuilder");
        var method = inlineBuilder
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(method =>
                method.Name == "TryCreateFormattedContent" &&
                method.GetParameters() is [{ ParameterType: var first }, { ParameterType: var second }, { IsOut: true }] &&
                first == typeof(FormattedString) &&
                second == typeof(ProTextRichStyle));

        object?[] parameters = [formattedText, baseStyle, null];
        var result = (bool)method.Invoke(null, parameters)!;

        Assert.True(result);
        return Assert.IsType<ProTextRichContent>(parameters[2]);
    }

    private static ProTextRichStyle CreateStyle(ProTextFontStyle fontStyle, int fontWeight)
    {
        return new ProTextRichStyle(
            ProTextFontDescriptor.DefaultFontFamily,
            12,
            fontStyle,
            fontWeight,
            5,
            new ProTextSolidBrush(ProTextColor.Black, 1),
            textDecorations: [],
            fontFeaturesFingerprint: "none",
            letterSpacing: 0);
    }
}
