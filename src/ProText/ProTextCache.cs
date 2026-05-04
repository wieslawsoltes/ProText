using System.Collections.Concurrent;
using Pretext;
using ProText.Internal;

namespace ProText;

/// <summary>
/// Provides process-wide cache controls for <see cref="ProTextBlock"/>.
/// </summary>
public static class ProTextCache
{
    private const int DefaultMaxEntryCount = 4096;

    private static readonly ConcurrentDictionary<ProTextCacheKey, PreparedTextWithSegments> s_preparedText = new();
    private static readonly ConcurrentDictionary<ProTextRichCacheKey, PreparedRichInline> s_preparedRichText = new();
    private static readonly ConcurrentQueue<ProTextCacheKey> s_insertionOrder = new();
    private static readonly ConcurrentQueue<ProTextRichCacheKey> s_richInsertionOrder = new();
    private static int s_configured;
    private static int s_maxEntryCount = DefaultMaxEntryCount;
    private static long s_hits;
    private static long s_misses;

    /// <summary>
    /// Gets or sets the maximum number of prepared text entries kept in the global cache.
    /// </summary>
    public static int MaxEntryCount
    {
        get => Volatile.Read(ref s_maxEntryCount);
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The cache size must be greater than zero.");
            }

            Volatile.Write(ref s_maxEntryCount, value);
            TrimToLimit();
        }
    }

    /// <summary>
    /// Clears the ProText cache and PretextSharp's internal font and segment caches.
    /// </summary>
    public static void Clear()
    {
        s_preparedText.Clear();
        s_preparedRichText.Clear();

        while (s_insertionOrder.TryDequeue(out _))
        {
        }

        while (s_richInsertionOrder.TryDequeue(out _))
        {
        }

        Interlocked.Exchange(ref s_hits, 0);
        Interlocked.Exchange(ref s_misses, 0);
        EnsureConfigured();
        PretextLayout.ClearCache();
    }

    /// <summary>
    /// Gets a snapshot of cache counters for diagnostics and benchmarks.
    /// </summary>
    public static ProTextCacheSnapshot GetSnapshot()
    {
        return new ProTextCacheSnapshot(
            s_preparedText.Count + s_preparedRichText.Count,
            MaxEntryCount,
            Interlocked.Read(ref s_hits),
            Interlocked.Read(ref s_misses));
    }

    internal static PreparedTextWithSegments GetOrPrepare(ProTextCacheKey key)
    {
        EnsureConfigured();

        if (s_preparedText.TryGetValue(key, out var cached))
        {
            Interlocked.Increment(ref s_hits);
            return cached;
        }

        Interlocked.Increment(ref s_misses);

        var prepared = s_preparedText.GetOrAdd(key, static cacheKey =>
        {
            var created = PretextLayout.PrepareWithSegments(
                cacheKey.Text,
                cacheKey.Font,
                new PrepareOptions(cacheKey.WhiteSpace, cacheKey.WordBreak));

            s_insertionOrder.Enqueue(cacheKey);
            TrimToLimit();
            return created;
        });

        return prepared;
    }

    internal static PreparedTextWithSegments PrepareUncached(ProTextCacheKey key)
    {
        EnsureConfigured();

        return PretextLayout.PrepareWithSegments(
            key.Text,
            key.Font,
            new PrepareOptions(key.WhiteSpace, key.WordBreak));
    }

    internal static PreparedRichInline GetOrPrepareRich(ProTextRichCacheKey key, IReadOnlyList<RichInlineItem> items)
    {
        EnsureConfigured();

        if (s_preparedRichText.TryGetValue(key, out var cached))
        {
            Interlocked.Increment(ref s_hits);
            return cached;
        }

        Interlocked.Increment(ref s_misses);

        return s_preparedRichText.GetOrAdd(key, static (cacheKey, state) =>
        {
            var created = PretextLayout.PrepareRichInline(state.Items);
            s_richInsertionOrder.Enqueue(cacheKey);
            TrimToLimit();
            return created;
        }, new RichPrepareState(items));
    }

    internal static PreparedRichInline PrepareRichUncached(IReadOnlyList<RichInlineItem> items)
    {
        EnsureConfigured();
        return PretextLayout.PrepareRichInline(items);
    }

    internal static void EnsureConfigured()
    {
        if (Interlocked.Exchange(ref s_configured, 1) == 0)
        {
            PretextLayout.SetTextMeasurerFactory(new ProTextTextMeasurerFactory());
        }
    }

    private static void TrimToLimit()
    {
        var maxEntryCount = MaxEntryCount;

        while (s_preparedText.Count > maxEntryCount && s_insertionOrder.TryDequeue(out var key))
        {
            s_preparedText.TryRemove(key, out _);
        }

        while (s_preparedRichText.Count > maxEntryCount && s_richInsertionOrder.TryDequeue(out var key))
        {
            s_preparedRichText.TryRemove(key, out _);
        }
    }

    private readonly record struct RichPrepareState(IReadOnlyList<RichInlineItem> Items);
}