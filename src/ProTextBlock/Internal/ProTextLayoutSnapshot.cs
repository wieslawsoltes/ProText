using Avalonia;
using Avalonia.Media;
using Pretext;

namespace ProTextBlock.Internal;

internal sealed class ProTextLayoutSnapshot
{
    public ProTextLayoutSnapshot(
        ProTextRichContent content,
        ProTextPreparedContent prepared,
        double maxWidth,
        double lineHeight,
        int maxLines,
        TextWrapping textWrapping,
        TextTrimming textTrimming)
    {
        Content = content;
        MaxWidth = maxWidth;
        LineHeight = lineHeight;
        MaxLines = maxLines;
        TextWrapping = textWrapping;
        TextTrimming = textTrimming;

        Lines = BuildLines(content, prepared, maxWidth, maxLines, textWrapping, textTrimming);
        LineCount = Lines.Count;
        Width = Lines.Count == 0 ? 0 : Lines.Max(static line => line.Width);
        Height = LineCount * lineHeight;
    }

    public ProTextRichContent Content { get; }

    public double MaxWidth { get; }

    public double LineHeight { get; }

    public int MaxLines { get; }

    public TextWrapping TextWrapping { get; }

    public TextTrimming TextTrimming { get; }

    public double Width { get; }

    public double Height { get; }

    public int LineCount { get; }

    public IReadOnlyList<ProTextLayoutLine> Lines { get; }

    public Size Size => new(Width, Height);

    public bool Matches(ProTextRichContent content, double maxWidth, double lineHeight, int maxLines, TextWrapping textWrapping, TextTrimming textTrimming)
    {
        return Content.RenderFingerprint.Equals(content.RenderFingerprint, StringComparison.Ordinal)
            && MaxWidth.Equals(maxWidth)
            && LineHeight.Equals(lineHeight)
            && MaxLines == maxLines
            && TextWrapping == textWrapping
            && ReferenceEquals(TextTrimming, textTrimming);
    }

    private static IReadOnlyList<ProTextLayoutLine> BuildLines(
        ProTextRichContent content,
        ProTextPreparedContent prepared,
        double maxWidth,
        int maxLines,
        TextWrapping textWrapping,
        TextTrimming textTrimming)
    {
        var allLines = new List<ProTextLayoutLine>();
        var isConstrained = !double.IsPositiveInfinity(maxWidth);
        var layoutWidth = isConstrained ? Math.Max(1, maxWidth) : double.PositiveInfinity;

        for (var paragraphIndex = 0; paragraphIndex < content.Paragraphs.Count; paragraphIndex++)
        {
            var paragraph = content.Paragraphs[paragraphIndex];

            if (paragraph.Runs.Count == 0)
            {
                allLines.Add(ProTextLayoutLine.Empty);
                continue;
            }

            var cursor = default(RichInlineCursor?);

            while (true)
            {
                var range = PretextLayout.LayoutNextRichInlineLineRange(prepared.Paragraphs[paragraphIndex], layoutWidth, cursor);

                if (range is null)
                {
                    break;
                }

                var line = PretextLayout.MaterializeRichInlineLineRange(prepared.Paragraphs[paragraphIndex], range);
                allLines.Add(CreateLine(paragraph, line));
                cursor = range.End;
            }
        }

        if (allLines.Count == 0)
        {
            return [];
        }

        var visibleLineLimit = ResolveVisibleLineLimit(maxLines, textWrapping, textTrimming);
        var needsTrimming = !ReferenceEquals(textTrimming, TextTrimming.None)
            && isConstrained
            && (allLines.Count > visibleLineLimit || allLines[Math.Min(visibleLineLimit, allLines.Count) - 1].Width > maxWidth);

        if (allLines.Count > visibleLineLimit)
        {
            allLines.RemoveRange(visibleLineLimit, allLines.Count - visibleLineLimit);
        }

        if (needsTrimming && allLines.Count > 0)
        {
            var lastIndex = allLines.Count - 1;
            allLines[lastIndex] = TrimLine(allLines[lastIndex], Math.Max(0, maxWidth), textTrimming);
        }

        return allLines;
    }

    private static int ResolveVisibleLineLimit(int maxLines, TextWrapping textWrapping, TextTrimming textTrimming)
    {
        if (maxLines > 0)
        {
            return maxLines;
        }

        if (textWrapping == TextWrapping.NoWrap && !ReferenceEquals(textTrimming, TextTrimming.None))
        {
            return 1;
        }

        return int.MaxValue;
    }

    private static ProTextLayoutLine CreateLine(ProTextRichParagraph paragraph, RichInlineLine line)
    {
        var fragments = new ProTextLayoutFragment[line.Fragments.Length];
        var x = 0d;

        for (var i = 0; i < line.Fragments.Length; i++)
        {
            var fragment = line.Fragments[i];
            var run = paragraph.Runs[fragment.ItemIndex];
            x += fragment.GapBefore;
            fragments[i] = new ProTextLayoutFragment(fragment.Text, run.Style, x, fragment.OccupiedWidth);
            x += fragment.OccupiedWidth;
        }

        return new ProTextLayoutLine(fragments, line.Width);
    }

    private static ProTextLayoutLine TrimLine(ProTextLayoutLine line, double targetWidth, TextTrimming textTrimming)
    {
        if (line.Fragments.Count == 0)
        {
            return line;
        }

        var source = line.Fragments.Select(static fragment => fragment with { }).ToList();
        var ellipsisStyle = source.Last(static fragment => fragment.Text.Length > 0).Style;
        var ellipsisWidth = MeasureText("…", ellipsisStyle);

        if (targetWidth <= ellipsisWidth)
        {
            return new ProTextLayoutLine([new ProTextLayoutFragment("…", ellipsisStyle, 0, ellipsisWidth)], ellipsisWidth);
        }

        while (source.Count > 0 && MeasureLineWidth(source) + ellipsisWidth > targetWidth)
        {
            var last = source[^1];

            if (last.Text.Length == 0)
            {
                source.RemoveAt(source.Count - 1);
                continue;
            }

            var trimmedText = ProTextGraphemeEnumerator.RemoveLastGrapheme(last.Text);

            if (ReferenceEquals(textTrimming, TextTrimming.WordEllipsis))
            {
                trimmedText = TrimToLastWordBoundary(trimmedText);
            }

            if (trimmedText.Length == 0)
            {
                source.RemoveAt(source.Count - 1);
            }
            else
            {
                source[^1] = last with { Text = trimmedText, Width = MeasureText(trimmedText, last.Style) };
            }
        }

        if (source.Count == 0)
        {
            return new ProTextLayoutLine([new ProTextLayoutFragment("…", ellipsisStyle, 0, ellipsisWidth)], ellipsisWidth);
        }

        source.Add(new ProTextLayoutFragment("…", ellipsisStyle, 0, ellipsisWidth));
        return NormalizeFragmentPositions(source);
    }

    private static string TrimToLastWordBoundary(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var index = text.Length - 1;

        while (index > 0 && !char.IsWhiteSpace(text[index]))
        {
            index--;
        }

        return index <= 0 ? text : text[..index].TrimEnd();
    }

    private static ProTextLayoutLine NormalizeFragmentPositions(IReadOnlyList<ProTextLayoutFragment> fragments)
    {
        var normalized = new ProTextLayoutFragment[fragments.Count];
        var x = 0d;

        for (var i = 0; i < fragments.Count; i++)
        {
            var fragment = fragments[i];
            normalized[i] = fragment with { X = x };
            x += fragment.Width;
        }

        return new ProTextLayoutLine(normalized, x);
    }

    private static double MeasureLineWidth(IReadOnlyList<ProTextLayoutFragment> fragments)
    {
        var width = 0d;

        foreach (var fragment in fragments)
        {
            width += fragment.Width;
        }

        return width;
    }

    private static double MeasureText(string text, ProTextRichStyle style)
    {
        return ProTextBlockTextMeasurerFactory.MeasureText(text, style.FontDescriptor);
    }
}

internal sealed record ProTextLayoutLine(IReadOnlyList<ProTextLayoutFragment> Fragments, double Width)
{
    public static ProTextLayoutLine Empty { get; } = new([], 0);
}

internal sealed record ProTextLayoutFragment(string Text, ProTextRichStyle Style, double X, double Width);