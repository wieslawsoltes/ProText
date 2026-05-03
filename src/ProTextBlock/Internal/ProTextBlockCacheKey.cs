using Pretext;

namespace ProTextBlock.Internal;

internal readonly record struct ProTextBlockCacheKey(
    string Text,
    string Font,
    WhiteSpaceMode WhiteSpace,
    WordBreakMode WordBreak);