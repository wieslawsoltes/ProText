using System.Text;

namespace ProTextBlock.Internal;

internal sealed class ProTextRichContentBuilder
{
    private readonly List<ProTextRichParagraph> _paragraphs = [];
    private readonly List<ProTextRichRun> _runs = [];
    private readonly StringBuilder _layoutFingerprint = new();
    private readonly StringBuilder _renderFingerprint = new();
    private readonly StringBuilder _text = new();
    private double _maxFontSize;

    public ProTextRichContentBuilder(ProTextRichStyle baseStyle)
    {
        BaseStyle = baseStyle;
        _maxFontSize = baseStyle.FontSize;
    }

    public ProTextRichStyle BaseStyle { get; }

    public void AppendText(string text, ProTextRichStyle style)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\r' && text[i] != '\n')
            {
                continue;
            }

            if (i > start)
            {
                AppendRun(text[start..i], style);
            }

            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                i++;
            }

            AppendLineBreak();
            start = i + 1;
        }

        if (start < text.Length)
        {
            AppendRun(text[start..], style);
        }
    }

    public void AppendLineBreak()
    {
        FlushParagraph(allowEmpty: true);
        _text.Append('\n');
    }

    public ProTextRichContent Build()
    {
        FlushParagraph(allowEmpty: false);

        return new ProTextRichContent(
            _paragraphs,
            _layoutFingerprint.ToString(),
            _renderFingerprint.ToString(),
            _maxFontSize,
            _text.ToString());
    }

    private void AppendRun(string text, ProTextRichStyle style)
    {
        if (text.Length == 0)
        {
            return;
        }

        var textStart = _text.Length;
        _runs.Add(new ProTextRichRun(text, style, textStart));
        _text.Append(text);
        _maxFontSize = Math.Max(_maxFontSize, style.FontSize);
    }

    private void FlushParagraph(bool allowEmpty)
    {
        if (!allowEmpty && _runs.Count == 0)
        {
            return;
        }

        var paragraphLayoutFingerprint = CreateParagraphFingerprint(_runs, render: false);
        var paragraphRenderFingerprint = CreateParagraphFingerprint(_runs, render: true);
        _paragraphs.Add(new ProTextRichParagraph(_runs.ToArray(), paragraphLayoutFingerprint, paragraphRenderFingerprint));

        if (_layoutFingerprint.Length > 0)
        {
            _layoutFingerprint.Append("|p|");
            _renderFingerprint.Append("|p|");
        }

        _layoutFingerprint.Append(paragraphLayoutFingerprint);
        _renderFingerprint.Append(paragraphRenderFingerprint);
        _runs.Clear();
    }

    private static string CreateParagraphFingerprint(IReadOnlyList<ProTextRichRun> runs, bool render)
    {
        var builder = new StringBuilder();

        foreach (var run in runs)
        {
            var styleFingerprint = render ? run.Style.RenderFingerprint : run.Style.FontDescriptor;
            builder.Append(styleFingerprint.Length);
            builder.Append(':');
            builder.Append(styleFingerprint);
            builder.Append('/');
            builder.Append(run.Text.Length);
            builder.Append(':');
            builder.Append(run.Text);
            builder.Append(';');
        }

        return builder.ToString();
    }
}