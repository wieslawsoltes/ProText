using Pretext;

namespace ProText.Core;

/// <summary>
/// Framework-neutral per-control layout and prepared-content cache for ProText rich content.
/// </summary>
public sealed class ProTextLayoutCache
{
    private ProTextLayoutSnapshot? _layoutSnapshot;
    private ProTextLayoutSnapshot? _previousLayoutSnapshot;
    private ProTextRichCacheKey? _preparedKey;
    private ProTextPreparedContent? _prepared;
    private bool _preparedUsesGlobalCache;

    public ProTextLayoutSnapshot? CurrentSnapshot => _layoutSnapshot;

    public void Clear()
    {
        _layoutSnapshot = null;
        _previousLayoutSnapshot = null;
        _preparedKey = null;
        _prepared = null;
        _preparedUsesGlobalCache = false;
    }

    public ProTextLayoutSnapshot GetSnapshot(ProTextRichContent content, ProTextLayoutRequest request)
    {
        if (_layoutSnapshot is { } snapshot && snapshot.MatchesLayout(
            content,
            request.MaxWidth,
            request.LineHeight,
            request.MaxLines,
            request.TextWrapping,
            request.TextTrimming))
        {
            snapshot = snapshot.WithRenderContent(content);
            _layoutSnapshot = snapshot;
            return snapshot;
        }

        if (_previousLayoutSnapshot is { } previousSnapshot && previousSnapshot.MatchesLayout(
            content,
            request.MaxWidth,
            request.LineHeight,
            request.MaxLines,
            request.TextWrapping,
            request.TextTrimming))
        {
            previousSnapshot = previousSnapshot.WithRenderContent(content);
            _previousLayoutSnapshot = _layoutSnapshot;
            _layoutSnapshot = previousSnapshot;
            return previousSnapshot;
        }

        var prepared = GetPreparedContent(content, request.UseGlobalCache);

        snapshot = new ProTextLayoutSnapshot(
            content,
            prepared,
            request.MaxWidth,
            request.LineHeight,
            request.MaxLines,
            request.TextWrapping,
            request.TextTrimming);

        _previousLayoutSnapshot = _layoutSnapshot;
        _layoutSnapshot = snapshot;
        return snapshot;
    }

    public ProTextPreparedContent GetPreparedContent(ProTextRichContent content, bool useGlobalCache)
    {
        var key = new ProTextRichCacheKey(content.LayoutFingerprint);

        if (_prepared is not null && _preparedKey == key && _preparedUsesGlobalCache == useGlobalCache)
        {
            return _prepared;
        }

        var preparedParagraphs = new PreparedRichInline[content.Paragraphs.Count];

        for (var i = 0; i < content.Paragraphs.Count; i++)
        {
            var paragraph = content.Paragraphs[i];
            var items = paragraph.CreateInlineItems();

            preparedParagraphs[i] = useGlobalCache
                ? ProTextCoreCache.GetOrPrepareRich(new ProTextRichCacheKey(paragraph.LayoutFingerprint), items)
                : ProTextCoreCache.PrepareRichUncached(items);
        }

        var prepared = new ProTextPreparedContent(preparedParagraphs);

        _preparedKey = key;
        _prepared = prepared;
        _preparedUsesGlobalCache = useGlobalCache;

        return prepared;
    }
}

public readonly record struct ProTextLayoutRequest(
    double MaxWidth,
    double LineHeight,
    int MaxLines,
    ProTextWrapping TextWrapping,
    ProTextTrimming TextTrimming,
    bool UseGlobalCache);
