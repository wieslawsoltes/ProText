using Pretext;
using ProText.Core;
using ProText.MAUI.Internal;

namespace ProText.MAUI;

/// <summary>
/// Provides process-wide cache controls for MAUI ProText controls.
/// </summary>
public static class ProTextCache
{
    /// <summary>
    /// Gets or sets the maximum number of prepared text entries kept in the global cache.
    /// </summary>
    public static int MaxEntryCount
    {
        get => ProTextCoreCache.MaxEntryCount;
        set => ProTextCoreCache.MaxEntryCount = value;
    }

    /// <summary>
    /// Clears the ProText cache and PretextSharp's internal font and segment caches.
    /// </summary>
    public static void Clear()
    {
        ProTextMauiPlatform.EnsureConfigured();
        ProTextCoreCache.Clear();
    }

    /// <summary>
    /// Gets a snapshot of cache counters for diagnostics and benchmarks.
    /// </summary>
    public static ProTextCacheSnapshot GetSnapshot()
    {
        var snapshot = ProTextCoreCache.GetSnapshot();
        return new ProTextCacheSnapshot(snapshot.Count, snapshot.MaxEntryCount, snapshot.Hits, snapshot.Misses);
    }

    internal static PreparedTextWithSegments GetOrPrepare(ProTextCacheKey key)
    {
        ProTextMauiPlatform.EnsureConfigured();
        return ProTextCoreCache.GetOrPrepare(key);
    }

    internal static PreparedTextWithSegments PrepareUncached(ProTextCacheKey key)
    {
        ProTextMauiPlatform.EnsureConfigured();
        return ProTextCoreCache.PrepareUncached(key);
    }

    internal static PreparedRichInline GetOrPrepareRich(ProTextRichCacheKey key, IReadOnlyList<RichInlineItem> items)
    {
        ProTextMauiPlatform.EnsureConfigured();
        return ProTextCoreCache.GetOrPrepareRich(key, items);
    }

    internal static PreparedRichInline PrepareRichUncached(IReadOnlyList<RichInlineItem> items)
    {
        ProTextMauiPlatform.EnsureConfigured();
        return ProTextCoreCache.PrepareRichUncached(items);
    }
}
