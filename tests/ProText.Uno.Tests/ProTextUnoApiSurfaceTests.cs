using System.Reflection;

namespace ProText.Uno.Tests;

public class ProTextUnoApiSurfaceTests
{
    [Fact]
    public void ExposesExpectedControls()
    {
        Assert.Equal("ProText.Uno.ProTextBlock", typeof(ProTextBlock).FullName);
        Assert.Equal("ProText.Uno.ProTextPresenter", typeof(ProTextPresenter).FullName);
        Assert.Equal("ProText.Uno.ProTextBox", typeof(ProTextBox).FullName);
    }

    [Fact]
    public void ProTextBlockExposesSharedCacheAndTextProperties()
    {
        var properties = typeof(ProTextBlock)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(static property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("Text", properties);
        Assert.Contains("Inlines", properties);
        Assert.Contains("UseGlobalCache", properties);
        Assert.Contains("UsePretextRendering", properties);
        Assert.Contains("PretextWhiteSpace", properties);
        Assert.Contains("PretextWordBreak", properties);
        Assert.Contains("PretextLineHeightMultiplier", properties);
        Assert.Contains("CharacterSpacing", properties);
        Assert.Contains("LetterSpacing", properties);
    }

    [Fact]
    public void ProTextPresenterExposesPresenterApis()
    {
        var methods = typeof(ProTextPresenter)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(static method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ShowCaret", methods);
        Assert.Contains("HideCaret", methods);
        Assert.Contains("MoveCaretToTextPosition", methods);
        Assert.Contains("MoveCaretToPoint", methods);
        Assert.Contains("MoveCaretHorizontal", methods);
        Assert.Contains("MoveCaretVertical", methods);
        Assert.Contains("GetCharacterIndex", methods);
        Assert.Contains("GetCaretBounds", methods);
    }

    [Fact]
    public void ProTextBoxExposesTextBoxLikeHelpers()
    {
        var methods = typeof(ProTextBox)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(static method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("Select", methods);
        Assert.Contains("SelectAll", methods);
        Assert.Contains("ClearSelection", methods);
        Assert.Contains("AppendText", methods);
        Assert.Contains("InsertTextAtCaret", methods);
        Assert.Contains("DeleteSelection", methods);
    }

    [Fact]
    public void CacheSnapshotIsFrameworkNeutralData()
    {
        var snapshot = new ProTextCacheSnapshot(1, 2, 3, 4);

        Assert.Equal(1, snapshot.Count);
        Assert.Equal(2, snapshot.MaxEntryCount);
        Assert.Equal(3, snapshot.Hits);
        Assert.Equal(4, snapshot.Misses);
    }
}
