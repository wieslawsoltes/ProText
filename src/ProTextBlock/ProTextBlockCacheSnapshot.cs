namespace ProTextBlock;

/// <summary>
/// Captures high-level diagnostic counters for the shared ProTextBlock prepared-text cache.
/// </summary>
public readonly record struct ProTextBlockCacheSnapshot(
    int Count,
    int MaxEntryCount,
    long Hits,
    long Misses);