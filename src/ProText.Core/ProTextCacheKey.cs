using Pretext;

namespace ProText.Core;

public readonly record struct ProTextCacheKey(
    string Text,
    string Font,
    WhiteSpaceMode WhiteSpace,
    WordBreakMode WordBreak);
