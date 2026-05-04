using Avalonia.Media;
using ProText.Core;
using ProText.Avalonia.Internal;

namespace ProText.Tests;

public sealed class ProTextAvaloniaAdapterTests
{
    [Fact]
    public void Extra_avalonia_ellipsis_modes_remain_trimming_modes()
    {
        Assert.Equal(ProTextTrimming.CharacterEllipsis, ProTextAvaloniaAdapter.ToCore(TextTrimming.PrefixCharacterEllipsis));
        Assert.Equal(ProTextTrimming.CharacterEllipsis, ProTextAvaloniaAdapter.ToCore(TextTrimming.LeadingCharacterEllipsis));
        Assert.Equal(ProTextTrimming.CharacterEllipsis, ProTextAvaloniaAdapter.ToCore(TextTrimming.PathSegmentEllipsis));
    }
}
