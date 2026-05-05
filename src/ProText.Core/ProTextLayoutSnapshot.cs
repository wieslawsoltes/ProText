using Pretext;

namespace ProText.Core;

/// <summary>
/// Width-local materialized layout snapshot built from prepared Pretext rich content.
/// </summary>
public sealed class ProTextLayoutSnapshot
{
    public ProTextLayoutSnapshot(
        ProTextRichContent content,
        ProTextPreparedContent prepared,
        double maxWidth,
        double lineHeight,
        int maxLines,
        ProTextWrapping textWrapping,
        ProTextTrimming textTrimming)
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

    private ProTextLayoutSnapshot(
        ProTextRichContent content,
        double maxWidth,
        double lineHeight,
        int maxLines,
        ProTextWrapping textWrapping,
        ProTextTrimming textTrimming,
        IReadOnlyList<ProTextLayoutLine> lines,
        double width,
        double height)
    {
        Content = content;
        MaxWidth = maxWidth;
        LineHeight = lineHeight;
        MaxLines = maxLines;
        TextWrapping = textWrapping;
        TextTrimming = textTrimming;
        Lines = lines;
        LineCount = lines.Count;
        Width = width;
        Height = height;
    }

    public ProTextRichContent Content { get; }

    public double MaxWidth { get; }

    public double LineHeight { get; }

    public int MaxLines { get; }

    public ProTextWrapping TextWrapping { get; }

    public ProTextTrimming TextTrimming { get; }

    public double Width { get; }

    public double Height { get; }

    public int LineCount { get; }

    public IReadOnlyList<ProTextLayoutLine> Lines { get; }

    public ProTextSize Size => new(Width, Height);

    public bool Matches(ProTextRichContent content, double maxWidth, double lineHeight, int maxLines, ProTextWrapping textWrapping, ProTextTrimming textTrimming)
    {
        return Content.RenderFingerprint.Equals(content.RenderFingerprint, StringComparison.Ordinal)
            && MaxWidth.Equals(maxWidth)
            && LineHeight.Equals(lineHeight)
            && MaxLines == maxLines
            && TextWrapping == textWrapping
            && TextTrimming == textTrimming;
    }

    public bool MatchesLayout(ProTextRichContent content, double maxWidth, double lineHeight, int maxLines, ProTextWrapping textWrapping, ProTextTrimming textTrimming)
    {
        return Content.LayoutFingerprint.Equals(content.LayoutFingerprint, StringComparison.Ordinal)
            && MaxWidth.Equals(maxWidth)
            && LineHeight.Equals(lineHeight)
            && MaxLines == maxLines
            && TextWrapping == textWrapping
            && TextTrimming == textTrimming;
    }

    public ProTextLayoutSnapshot WithRenderContent(ProTextRichContent content)
    {
        if (Content.RenderFingerprint.Equals(content.RenderFingerprint, StringComparison.Ordinal))
        {
            return this;
        }

        if (!Content.LayoutFingerprint.Equals(content.LayoutFingerprint, StringComparison.Ordinal))
        {
            throw new ArgumentException("Render content must have the same layout fingerprint.", nameof(content));
        }

        var lines = new ProTextLayoutLine[Lines.Count];

        for (var i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            var fragments = new ProTextLayoutFragment[line.Fragments.Count];

            for (var j = 0; j < fragments.Length; j++)
            {
                var fragment = line.Fragments[j];
                var style = ResolveRenderStyle(content, fragment);
                fragments[j] = fragment with { Style = style };
            }

            lines[i] = line with { Fragments = fragments };
        }

        return new ProTextLayoutSnapshot(content, MaxWidth, LineHeight, MaxLines, TextWrapping, TextTrimming, lines, Width, Height);
    }

    private static IReadOnlyList<ProTextLayoutLine> BuildLines(
        ProTextRichContent content,
        ProTextPreparedContent prepared,
        double maxWidth,
        int maxLines,
        ProTextWrapping textWrapping,
        ProTextTrimming textTrimming)
    {
        var allLines = new List<ProTextLayoutLine>();
        var isConstrained = !double.IsPositiveInfinity(maxWidth);
        var layoutWidth = isConstrained && (textWrapping != ProTextWrapping.NoWrap || textTrimming == ProTextTrimming.None)
            ? Math.Max(1, maxWidth)
            : double.PositiveInfinity;

        for (var paragraphIndex = 0; paragraphIndex < content.Paragraphs.Count; paragraphIndex++)
        {
            var paragraph = content.Paragraphs[paragraphIndex];

            if (paragraph.Runs.Count == 0)
            {
                allLines.Add(new ProTextLayoutLine([], 0, 0, 0));
                continue;
            }

            var cursor = default(RichInlineCursor?);
            var runSearchOffsets = new int[paragraph.Runs.Count];

            while (true)
            {
                var range = PretextLayout.LayoutNextRichInlineLineRange(prepared.Paragraphs[paragraphIndex], layoutWidth, cursor);

                if (range is null)
                {
                    break;
                }

                var line = PretextLayout.MaterializeRichInlineLineRange(prepared.Paragraphs[paragraphIndex], range);
                allLines.Add(CreateLine(paragraphIndex, paragraph, line, runSearchOffsets));
                cursor = range.End;
            }
        }

        if (allLines.Count == 0)
        {
            return [];
        }

        var visibleLineLimit = ResolveVisibleLineLimit(maxLines, textWrapping, textTrimming);
        var needsTrimming = textTrimming != ProTextTrimming.None
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

    private static int ResolveVisibleLineLimit(int maxLines, ProTextWrapping textWrapping, ProTextTrimming textTrimming)
    {
        if (maxLines > 0)
        {
            return maxLines;
        }

        if (textWrapping == ProTextWrapping.NoWrap && textTrimming != ProTextTrimming.None)
        {
            return 1;
        }

        return int.MaxValue;
    }

    private static ProTextLayoutLine CreateLine(int paragraphIndex, ProTextRichParagraph paragraph, RichInlineLine line, int[] runSearchOffsets)
    {
        var fragments = new ProTextLayoutFragment[line.Fragments.Length];
        var x = 0d;
        var lineStart = int.MaxValue;
        var lineEnd = 0;

        for (var i = 0; i < line.Fragments.Length; i++)
        {
            var fragment = line.Fragments[i];
            var run = paragraph.Runs[fragment.ItemIndex];
            x += fragment.GapBefore;
            var runOffset = FindFragmentOffset(run.Text, fragment.Text, runSearchOffsets[fragment.ItemIndex]);
            runSearchOffsets[fragment.ItemIndex] = Math.Min(run.Text.Length, runOffset + fragment.Text.Length);
            var textStart = run.TextStart + runOffset;
            var textEnd = textStart + fragment.Text.Length;
            fragments[i] = new ProTextLayoutFragment(fragment.Text, run.Style, x, fragment.OccupiedWidth, textStart, fragment.Text.Length, paragraphIndex, fragment.ItemIndex);
            lineStart = Math.Min(lineStart, textStart);
            lineEnd = Math.Max(lineEnd, textEnd);
            x += fragment.OccupiedWidth;
        }

        if (lineStart == int.MaxValue)
        {
            lineStart = lineEnd;
        }

        return new ProTextLayoutLine(fragments, line.Width, lineStart, lineEnd);
    }

    private static int FindFragmentOffset(string runText, string fragmentText, int start)
    {
        if (fragmentText.Length == 0)
        {
            return Math.Min(start, runText.Length);
        }

        var index = runText.IndexOf(fragmentText, Math.Min(start, runText.Length), StringComparison.Ordinal);
        return index >= 0 ? index : Math.Min(start, runText.Length);
    }

    private static ProTextLayoutLine TrimLine(ProTextLayoutLine line, double targetWidth, ProTextTrimming textTrimming)
    {
        if (line.Fragments.Count == 0)
        {
            return line;
        }

        return textTrimming switch
        {
            ProTextTrimming.HeadCharacterEllipsis => TrimLineHead(line, targetWidth),
            ProTextTrimming.MiddleCharacterEllipsis => TrimLineMiddle(line, targetWidth),
            _ => TrimLineTail(line, targetWidth, textTrimming),
        };
    }

    private static ProTextLayoutLine TrimLineTail(ProTextLayoutLine line, double targetWidth, ProTextTrimming textTrimming)
    {
        var source = CloneFragments(line.Fragments);
        var ellipsisStyle = source.Last(static fragment => fragment.Text.Length > 0).Style;
        var ellipsisWidth = MeasureText("…", ellipsisStyle);

        if (targetWidth <= ellipsisWidth)
        {
            return CreateEllipsisOnlyLine(line, ellipsisStyle, ellipsisWidth);
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

            if (textTrimming == ProTextTrimming.WordEllipsis)
            {
                trimmedText = TrimToLastWordBoundary(trimmedText);
            }

            if (trimmedText.Length == 0)
            {
                source.RemoveAt(source.Count - 1);
            }
            else
            {
                source[^1] = last with { Text = trimmedText, TextLength = trimmedText.Length, Width = MeasureText(trimmedText, last.Style) };
            }
        }

        if (source.Count == 0)
        {
            return CreateEllipsisOnlyLine(line, ellipsisStyle, ellipsisWidth);
        }

        var ellipsisSource = source[^1];
        source.Add(new ProTextLayoutFragment(
            "…",
            ellipsisStyle,
            0,
            ellipsisWidth,
            ellipsisSource.TextEnd,
            0,
            ellipsisSource.ParagraphIndex,
            ellipsisSource.RunIndex));
        return NormalizeFragmentPositions(source);
    }

    private static ProTextLayoutLine TrimLineHead(ProTextLayoutLine line, double targetWidth)
    {
        var source = CloneFragments(line.Fragments);
        var ellipsisStyle = source.First(static fragment => fragment.Text.Length > 0).Style;
        var ellipsisWidth = MeasureText("…", ellipsisStyle);

        if (targetWidth <= ellipsisWidth)
        {
            return CreateEllipsisOnlyLine(line, ellipsisStyle, ellipsisWidth);
        }

        while (source.Count > 0 && MeasureLineWidth(source) + ellipsisWidth > targetWidth)
        {
            RemoveFirstVisibleGrapheme(source);
        }

        if (source.Count == 0)
        {
            return CreateEllipsisOnlyLine(line, ellipsisStyle, ellipsisWidth);
        }

        var ellipsisSource = line.Fragments.First(static fragment => fragment.Text.Length > 0);
        source.Insert(0, new ProTextLayoutFragment(
            "…",
            ellipsisStyle,
            0,
            ellipsisWidth,
            line.StartTextIndex,
            0,
            ellipsisSource.ParagraphIndex,
            ellipsisSource.RunIndex));
        return NormalizeFragmentPositions(source);
    }

    private static ProTextLayoutLine TrimLineMiddle(ProTextLayoutLine line, double targetWidth)
    {
        var source = CloneFragments(line.Fragments);
        var ellipsisStyle = source[Math.Max(0, source.Count / 2)].Style;
        var ellipsisWidth = MeasureText("…", ellipsisStyle);

        if (targetWidth <= ellipsisWidth)
        {
            return CreateEllipsisOnlyLine(line, ellipsisStyle, ellipsisWidth);
        }

        SplitFragmentsForMiddleTrimming(source, out var left, out var right);

        while (left.Count + right.Count > 0 && MeasureLineWidth(left) + ellipsisWidth + MeasureLineWidth(right) > targetWidth)
        {
            if (MeasureLineWidth(left) >= MeasureLineWidth(right) && left.Count > 0)
            {
                RemoveLastVisibleGrapheme(left);
            }
            else if (right.Count > 0)
            {
                RemoveFirstVisibleGrapheme(right);
            }
            else
            {
                RemoveLastVisibleGrapheme(left);
            }
        }

        if (left.Count == 0 && right.Count == 0)
        {
            return CreateEllipsisOnlyLine(line, ellipsisStyle, ellipsisWidth);
        }

        var ellipsisSource = left.LastOrDefault(static fragment => fragment.Text.Length > 0)
            ?? right.First(static fragment => fragment.Text.Length > 0);
        var fragments = new List<ProTextLayoutFragment>(left.Count + 1 + right.Count);
        fragments.AddRange(left);
        fragments.Add(new ProTextLayoutFragment(
            "…",
            ellipsisStyle,
            0,
            ellipsisWidth,
            ellipsisSource.TextEnd,
            0,
            ellipsisSource.ParagraphIndex,
            ellipsisSource.RunIndex));
        fragments.AddRange(right);
        return NormalizeFragmentPositions(fragments);
    }

    private static List<ProTextLayoutFragment> CloneFragments(IReadOnlyList<ProTextLayoutFragment> fragments)
    {
        return fragments.Select(static fragment => fragment with { }).ToList();
    }

    private static ProTextLayoutLine CreateEllipsisOnlyLine(ProTextLayoutLine line, ProTextRichStyle ellipsisStyle, double ellipsisWidth)
    {
        var sourceFragment = line.Fragments.FirstOrDefault(static fragment => fragment.Text.Length > 0)
            ?? line.Fragments[0];
        return new ProTextLayoutLine(
            [new ProTextLayoutFragment("…", ellipsisStyle, 0, ellipsisWidth, line.StartTextIndex, 0, sourceFragment.ParagraphIndex, sourceFragment.RunIndex)],
            ellipsisWidth,
            line.StartTextIndex,
            line.StartTextIndex);
    }

    private static void RemoveFirstVisibleGrapheme(List<ProTextLayoutFragment> fragments)
    {
        while (fragments.Count > 0)
        {
            var first = fragments[0];

            if (first.Text.Length == 0)
            {
                fragments.RemoveAt(0);
                continue;
            }

            var trimmedText = ProTextGraphemeEnumerator.RemoveFirstGrapheme(first.Text);

            if (trimmedText.Length == 0)
            {
                fragments.RemoveAt(0);
            }
            else
            {
                var removedLength = first.Text.Length - trimmedText.Length;
                fragments[0] = first with
                {
                    Text = trimmedText,
                    Width = MeasureText(trimmedText, first.Style),
                    TextStart = first.TextStart + removedLength,
                    TextLength = trimmedText.Length,
                };
            }

            return;
        }
    }

    private static void RemoveLastVisibleGrapheme(List<ProTextLayoutFragment> fragments)
    {
        while (fragments.Count > 0)
        {
            var lastIndex = fragments.Count - 1;
            var last = fragments[lastIndex];

            if (last.Text.Length == 0)
            {
                fragments.RemoveAt(lastIndex);
                continue;
            }

            var trimmedText = ProTextGraphemeEnumerator.RemoveLastGrapheme(last.Text);

            if (trimmedText.Length == 0)
            {
                fragments.RemoveAt(lastIndex);
            }
            else
            {
                fragments[lastIndex] = last with
                {
                    Text = trimmedText,
                    Width = MeasureText(trimmedText, last.Style),
                    TextLength = trimmedText.Length,
                };
            }

            return;
        }
    }

    private static void SplitFragmentsForMiddleTrimming(
        IReadOnlyList<ProTextLayoutFragment> source,
        out List<ProTextLayoutFragment> left,
        out List<ProTextLayoutFragment> right)
    {
        left = [];
        right = [];

        var totalGraphemes = source.Sum(static fragment => ProTextGraphemeEnumerator.Count(fragment.Text));
        var remainingLeftGraphemes = Math.Max(1, totalGraphemes / 2);

        foreach (var fragment in source)
        {
            var fragmentGraphemes = ProTextGraphemeEnumerator.Count(fragment.Text);

            if (fragmentGraphemes == 0)
            {
                continue;
            }

            if (remainingLeftGraphemes >= fragmentGraphemes)
            {
                left.Add(fragment);
                remainingLeftGraphemes -= fragmentGraphemes;
                continue;
            }

            if (remainingLeftGraphemes <= 0)
            {
                right.Add(fragment);
                continue;
            }

            var split = ProTextGraphemeEnumerator.SplitAtGraphemeCount(fragment.Text, remainingLeftGraphemes);
            left.Add(fragment with
            {
                Text = split.Left,
                Width = MeasureText(split.Left, fragment.Style),
                TextLength = split.Left.Length,
            });
            right.Add(fragment with
            {
                Text = split.Right,
                Width = MeasureText(split.Right, fragment.Style),
                TextStart = fragment.TextStart + split.Left.Length,
                TextLength = split.Right.Length,
            });
            remainingLeftGraphemes = 0;
        }
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

        var start = normalized.Length == 0 ? 0 : normalized.Min(static fragment => fragment.TextStart);
        var end = normalized.Length == 0 ? start : normalized.Max(static fragment => fragment.TextEnd);
        return new ProTextLayoutLine(normalized, x, start, end);
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
        return ProTextRenderFontCache.MeasureText(text, style);
    }

    private static ProTextRichStyle ResolveRenderStyle(ProTextRichContent content, ProTextLayoutFragment fragment)
    {
        if (fragment.ParagraphIndex >= 0
            && fragment.ParagraphIndex < content.Paragraphs.Count
            && fragment.RunIndex >= 0
            && fragment.RunIndex < content.Paragraphs[fragment.ParagraphIndex].Runs.Count)
        {
            return content.Paragraphs[fragment.ParagraphIndex].Runs[fragment.RunIndex].Style;
        }

        return fragment.Style;
    }
}

public sealed record ProTextLayoutLine(IReadOnlyList<ProTextLayoutFragment> Fragments, double Width, int StartTextIndex, int EndTextIndex)
{
    public static ProTextLayoutLine Empty { get; } = new([], 0, 0, 0);
}

public sealed record ProTextLayoutFragment(
    string Text,
    ProTextRichStyle Style,
    double X,
    double Width,
    int TextStart,
    int TextLength,
    int ParagraphIndex = -1,
    int RunIndex = -1)
{
    public int TextEnd => TextStart + TextLength;
}

public readonly record struct ProTextSelectionRect(int LineIndex, ProTextRect Bounds);
