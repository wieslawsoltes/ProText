namespace ProText.MAUI;

/// <summary>
/// Captures high-level diagnostic counters for the shared ProText prepared-text cache.
/// </summary>
public readonly record struct ProTextCacheSnapshot(
    int Count,
    int MaxEntryCount,
    long Hits,
    long Misses);
