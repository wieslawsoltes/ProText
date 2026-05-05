using Microsoft.UI.Xaml;
using ProText.Core;
using ProText.Uno.Internal;
using Windows.UI.Text;

namespace ProText.Uno.Tests;

public class ProTextUnoAdapterTests
{
    [Fact]
    public void MapsTextWrappingToCore()
    {
        Assert.Equal(ProTextWrapping.NoWrap, ProTextUnoAdapter.ToCore(TextWrapping.NoWrap));
        Assert.Equal(ProTextWrapping.Wrap, ProTextUnoAdapter.ToCore(TextWrapping.Wrap));
        Assert.Equal(ProTextWrapping.Wrap, ProTextUnoAdapter.ToCore(TextWrapping.WrapWholeWords));
    }

    [Fact]
    public void MapsTextTrimmingToCore()
    {
        Assert.Equal(ProTextTrimming.None, ProTextUnoAdapter.ToCore(TextTrimming.None));
        Assert.Equal(ProTextTrimming.None, ProTextUnoAdapter.ToCore(TextTrimming.Clip));
        Assert.Equal(ProTextTrimming.CharacterEllipsis, ProTextUnoAdapter.ToCore(TextTrimming.CharacterEllipsis));
        Assert.Equal(ProTextTrimming.WordEllipsis, ProTextUnoAdapter.ToCore(TextTrimming.WordEllipsis));
    }

    [Fact]
    public void MapsTextDecorationsToImmutableCoreSnapshot()
    {
        var snapshot = ProTextUnoAdapter.SnapshotDecorations(TextDecorations.Underline | TextDecorations.Strikethrough);

        Assert.Equal(2, snapshot.Count);
        Assert.Contains(snapshot, decoration => decoration.Location == ProTextDecorationLocation.Underline);
        Assert.Contains(snapshot, decoration => decoration.Location == ProTextDecorationLocation.Strikethrough);
    }

    [Fact]
    public void NormalizesWinUILineHeight()
    {
        Assert.True(double.IsNaN(ProTextUnoAdapter.NormalizeLineHeight(0)));
        Assert.True(double.IsNaN(ProTextUnoAdapter.NormalizeLineHeight(double.PositiveInfinity)));
        Assert.Equal(21, ProTextUnoAdapter.NormalizeLineHeight(21));
    }

    [Fact]
    public void ConvertsCharacterSpacingToCoreLetterSpacing()
    {
        Assert.Equal(2.4, ProTextUnoAdapter.ToLetterSpacing(16, 150, 0), precision: 6);
        Assert.Equal(3.4, ProTextUnoAdapter.ToLetterSpacing(16, 150, 1), precision: 6);
    }
}
