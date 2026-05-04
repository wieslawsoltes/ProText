using Pretext;
using SkiaSharp;

namespace ProText.Core;

public sealed class ProTextTextMeasurerFactory : IPretextTextMeasurerFactory
{
    public string Name => "ProText.SkiaSharp";

    public bool IsSupported => true;

    public int Priority => 0;

    public IPretextTextMeasurer Create(string font)
    {
        if (font is null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        var descriptor = PretextFontParser.Parse(font);
        var family = PretextFontParser.MapGenericFamily(
            descriptor.PrimaryFamily,
            sansSerifFallback: "Arial",
            serifFallback: "Times New Roman",
            monospaceFallback: "Menlo");
        var weight = descriptor.Weight;
        var stretch = ProTextFontDescriptor.GetFontStretch(font);
        var style = descriptor.Italic ? ProTextFontStyle.Italic : ProTextFontStyle.Normal;
        var fontStyle = ProTextFontResolver.CreateFontStyle(weight, stretch, style);
        var resolvedTypeface = ProTextFontResolver.ResolveTypeface(family, weight, stretch, style);

        return new SkiaTextMeasurer(
            family,
            fontStyle,
            resolvedTypeface.Typeface,
            resolvedTypeface.OwnsTypeface,
            resolvedTypeface.Simulations,
            descriptor.Size,
            ProTextFontDescriptor.GetLetterSpacing(font));
    }

    internal static double MeasureText(string text, string font)
    {
        using var measurer = (SkiaTextMeasurer)new ProTextTextMeasurerFactory().Create(font);
        return measurer.MeasureText(text);
    }

    private sealed class SkiaTextMeasurer : IPretextTextMeasurer
    {
        private readonly SKTypeface _typeface;
        private readonly SKFont _font;
        private readonly string _family;
        private readonly SKFontStyle _fontStyle;
        private readonly bool _ownsTypeface;
        private readonly double _fontSize;
        private readonly double _letterSpacing;

        public SkiaTextMeasurer(
            string family,
            SKFontStyle fontStyle,
            SKTypeface typeface,
            bool ownsTypeface,
            ProTextFontSimulations simulations,
            double fontSize,
            double letterSpacing)
        {
            _family = family;
            _fontStyle = fontStyle;
            _typeface = typeface;
            _ownsTypeface = ownsTypeface;
            _fontSize = fontSize;
            _font = ProTextFontResolver.CreateFont(_typeface, fontSize, simulations);
            _letterSpacing = letterSpacing;
        }

        public double MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var width = _typeface.ContainsGlyphs(text)
                ? _font.MeasureText(text)
                : MeasureTextWithFallback(text);
            var spacingCount = Math.Max(0, ProTextGraphemeEnumerator.Count(text) - 1);
            return Math.Max(0, width + spacingCount * _letterSpacing);
        }

        private double MeasureTextWithFallback(string text)
        {
            var width = 0d;

            foreach (var grapheme in ProTextFontResolver.EnumerateGraphemes(text))
            {
                using var resolved = ProTextFontResolver.ResolveTypeface(_typeface, _family, _fontStyle, grapheme);
                using var font = ReferenceEquals(resolved.Typeface, _typeface)
                    ? null
                    : ProTextFontResolver.CreateFont(resolved.Typeface, _fontSize);
                width += (font ?? _font).MeasureText(grapheme);
            }

            return width;
        }

        public void Dispose()
        {
            _font.Dispose();

            if (_ownsTypeface)
            {
                _typeface.Dispose();
            }
        }
    }
}
