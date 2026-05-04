using Pretext;

namespace ProText.Internal;

internal readonly record struct ProTextCacheKey(
    string Text,
    string Font,
    WhiteSpaceMode WhiteSpace,
    WordBreakMode WordBreak);