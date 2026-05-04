using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using ProTextPresenterControl = ProText.Avalonia.ProTextPresenter;

namespace ProText.Tests;

public sealed class AvaloniaAdaptedTextPresenterTests
{
    [AvaloniaFact]
    public void TextPresenter_Can_Contain_Null_With_Password_Char_Set()
    {
        var presenter = new ProTextPresenterControl { PasswordChar = '*' };

        presenter.Measure(Size.Infinity);

        Assert.True(presenter.DesiredSize.Width >= 0);
        Assert.True(presenter.DesiredSize.Height >= 0);
    }

    [AvaloniaFact]
    public void TextPresenter_Can_Contain_Null_WithOut_Password_Char_Set()
    {
        var presenter = new ProTextPresenterControl();

        presenter.Measure(Size.Infinity);

        Assert.True(presenter.DesiredSize.Width >= 0);
        Assert.True(presenter.DesiredSize.Height >= 0);
    }

    [AvaloniaFact]
    public void Text_Presenter_Replaces_Formatted_Text_With_Password_Char()
    {
        var password = new ProTextPresenterControl
        {
            PasswordChar = '*',
            Text = "Test",
            FontSize = 20,
            LineHeight = 28
        };
        var masked = new ProTextPresenterControl
        {
            Text = "****",
            FontSize = 20,
            LineHeight = 28
        };

        password.Measure(Size.Infinity);
        masked.Measure(Size.Infinity);

        Assert.Equal(masked.DesiredSize.Width, password.DesiredSize.Width, precision: 3);
        Assert.Equal(masked.DesiredSize.Height, password.DesiredSize.Height, precision: 3);
    }

    [AvaloniaTheory]
    [InlineData(FontStretch.Condensed)]
    [InlineData(FontStretch.Expanded)]
    [InlineData(FontStretch.Normal)]
    [InlineData(FontStretch.ExtraCondensed)]
    [InlineData(FontStretch.SemiCondensed)]
    [InlineData(FontStretch.ExtraExpanded)]
    [InlineData(FontStretch.SemiExpanded)]
    [InlineData(FontStretch.UltraCondensed)]
    [InlineData(FontStretch.UltraExpanded)]
    public void TextPresenter_Should_Use_FontStretch_Property(FontStretch fontStretch)
    {
        var presenter = new ProTextPresenterControl
        {
            FontStretch = fontStretch,
            Text = "test",
            FontSize = 18,
            LineHeight = 26
        };

        presenter.Measure(Size.Infinity);

        Assert.Equal(fontStretch, presenter.FontStretch);
        Assert.True(presenter.DesiredSize.Width > 0);
    }

    [AvaloniaFact]
    public void Measure_And_Arrange_Should_Use_Trailing_Whitespace_For_Bounds()
    {
        var presenter = new ProTextPresenterControl
        {
            Text = "fy  ",
            FontStyle = FontStyle.Italic,
            FontSize = 48,
            LineHeight = 58,
            UseLayoutRounding = false
        };

        presenter.Measure(Size.Infinity);
        presenter.Arrange(new Rect(default, presenter.DesiredSize));

        Assert.True(presenter.DesiredSize.Width > 0);
        Assert.Equal(new Rect(default, presenter.DesiredSize), presenter.Bounds);
    }
}
