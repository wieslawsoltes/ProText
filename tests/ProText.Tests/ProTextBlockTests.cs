using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using ProText.Avalonia.Internal;
using ProTextCacheApi = ProText.Avalonia.ProTextCache;
using ProTextBlockControl = ProText.Avalonia.ProTextBlock;
using SkiaSharp;

namespace ProText.Tests;

public sealed class ProTextBlockTests
{
    [AvaloniaFact]
    public void Wrapped_plain_text_uses_pretext_measurement()
    {
        var control = new ProTextBlockControl
        {
            Text = "Alpha beta gamma delta epsilon zeta eta theta iota kappa lambda.",
            FontSize = 16,
            LineHeight = 22,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };

        control.Measure(new Size(180, double.PositiveInfinity));

        Assert.True(control.DesiredSize.Width <= 180.1);
        Assert.True(control.DesiredSize.Height >= 44);
    }

    [AvaloniaFact]
    public void Max_lines_clamps_pretext_height()
    {
        var unclamped = new ProTextBlockControl
        {
            Text = "One two three four five six seven eight nine ten eleven twelve thirteen fourteen.",
            FontSize = 16,
            LineHeight = 20,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };

        var clamped = new ProTextBlockControl
        {
            Text = unclamped.Text,
            FontSize = 16,
            LineHeight = 20,
            MaxLines = 1,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };

        var constraint = new Size(90, double.PositiveInfinity);
        unclamped.Measure(constraint);
        clamped.Measure(constraint);

        Assert.True(unclamped.DesiredSize.Height > clamped.DesiredSize.Height);
        Assert.Equal(20, clamped.DesiredSize.Height, precision: 3);
    }

    [AvaloniaFact]
    public void Global_cache_is_shared_across_controls()
    {
        ProTextCacheApi.Clear();

        var first = CreateCacheProbe();
        var second = CreateCacheProbe();

        first.Measure(new Size(220, double.PositiveInfinity));
        second.Measure(new Size(220, double.PositiveInfinity));

        var snapshot = ProTextCacheApi.GetSnapshot();
        Assert.True(snapshot.Count >= 1);
        Assert.True(snapshot.Hits >= 1);
        Assert.True(snapshot.Misses >= 1);
    }

    [AvaloniaFact]
    public void Per_control_cache_bypass_does_not_fill_global_cache()
    {
        ProTextCacheApi.Clear();

        var control = CreateCacheProbe();
        control.UseGlobalCache = false;
        control.Measure(new Size(220, double.PositiveInfinity));

        var snapshot = ProTextCacheApi.GetSnapshot();
        Assert.Equal(0, snapshot.Count);
        Assert.Equal(0, snapshot.Hits);
        Assert.Equal(0, snapshot.Misses);
    }

    [AvaloniaFact]
    public void Global_cache_ignores_render_only_style_changes()
    {
        ProTextCacheApi.Clear();

        var first = CreateCacheProbe();
        first.Foreground = Brushes.Red;

        var second = CreateCacheProbe();
        second.Foreground = Brushes.Blue;

        first.Measure(new Size(220, double.PositiveInfinity));
        second.Measure(new Size(220, double.PositiveInfinity));

        var snapshot = ProTextCacheApi.GetSnapshot();
        Assert.Equal(first.DesiredSize, second.DesiredSize);
        Assert.Equal(1, snapshot.Count);
        Assert.True(snapshot.Hits >= 1);
        Assert.True(snapshot.Misses >= 1);
    }

    [AvaloniaFact]
    public void Inline_content_uses_pretext_rich_path()
    {
        var control = new ProTextBlockControl
        {
            FontSize = 16,
            LineHeight = 22,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };

        control.Inlines!.Add(new Run("Inline "));
        control.Inlines.Add(new Bold { Inlines = { new Run("fallback") } });
        control.Inlines.Add(new Run(" stays compatible."));
        control.Measure(new Size(200, double.PositiveInfinity));

        Assert.False(control.IsUsingFallback);
        Assert.True(control.DesiredSize.Width > 0);
        Assert.True(control.DesiredSize.Height > 0);
    }

    [AvaloniaFact]
    public void Inline_ui_container_does_not_use_textblock_fallback()
    {
        var control = new ProTextBlockControl
        {
            FontSize = 16,
            LineHeight = 22,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };

        control.Inlines!.Add(new Run("Inline text before "));
        control.Inlines.Add(new InlineUIContainer(new Button { Content = "UI" }));
        control.Inlines.Add(new Run(" and after embedded UI."));

        control.Measure(new Size(220, double.PositiveInfinity));

        Assert.False(control.IsUsingFallback);
        Assert.True(control.DesiredSize.Width > 0);
        Assert.True(control.DesiredSize.Height > 0);
    }

    [AvaloniaFact]
    public void Rich_features_use_pretext_path()
    {
        var control = new ProTextBlockControl
        {
            Text = "Ligatures, spacing, underline, gradient foreground, and ellipsis stay on the Pretext path.",
            FontSize = 18,
            LineHeight = 24,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextDecorations = TextDecorations.Underline,
            FontFeatures = FontFeatureCollection.Parse("kern, liga"),
            LetterSpacing = 1.5,
            Foreground = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Colors.MediumVioletRed, 0),
                    new GradientStop(Colors.DeepSkyBlue, 1)
                }
            }
        };

        control.Measure(new Size(210, double.PositiveInfinity));

        Assert.False(control.IsUsingFallback);
        Assert.True(control.DesiredSize.Width <= 210.1);
        Assert.Equal(24, control.DesiredSize.Height, precision: 3);
    }

    [AvaloniaFact]
    public void Italic_font_style_resolves_italic_or_simulated_skia_typeface()
    {
        using var resolvedTypeface = global::ProText.Avalonia.Internal.ProTextFontResolver.ResolveTypeface(
            FontFamily.Default,
            FontWeight.Normal,
            FontStretch.Normal,
            FontStyle.Italic);

        Assert.True(
            resolvedTypeface.Typeface.FontSlant is SKFontStyleSlant.Italic or SKFontStyleSlant.Oblique ||
            (resolvedTypeface.Simulations & ProText.Core.ProTextFontSimulations.Oblique) != 0);
    }

    [AvaloniaFact]
    public void Multilingual_text_stays_on_pretext_path()
    {
        var control = new ProTextBlockControl
        {
            Text = "Hello world. Привет мир. مرحبا بالعالم. こんにちは世界。",
            FontSize = 16,
            LineHeight = 22,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };

        control.Measure(new Size(240, double.PositiveInfinity));

        Assert.False(control.IsUsingFallback);
        Assert.True(control.DesiredSize.Width > 0);
        Assert.True(control.DesiredSize.Height > 0);
    }

    [AvaloniaFact]
    public void Headless_window_renders_textblock_and_protextblock()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            Margin = new Thickness(16),
            ColumnSpacing = 16
        };

        grid.Children.Add(new TextBlock
        {
            Text = "Avalonia TextBlock baseline renders in the left column.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 18,
            LineHeight = 25,
            Foreground = Brushes.Black
        });

        var pro = new ProTextBlockControl
        {
            Text = "ProTextBlock renders through the PretextSharp path in the right column.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 18,
            LineHeight = 25,
            Foreground = Brushes.Black
        };

        Grid.SetColumn(pro, 1);
        grid.Children.Add(pro);

        var window = new Window
        {
            Width = 640,
            Height = 320,
            Content = grid,
            Background = Brushes.White
        };

        window.Show();

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
    }

    [AvaloniaFact]
    public void Headless_window_renders_rich_protextblock()
    {
        var control = new ProTextBlockControl
        {
            FontSize = 20,
            LineHeight = 28,
            MaxLines = 2,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextDecorations = TextDecorations.Underline,
            LetterSpacing = 1,
            FontFeatures = FontFeatureCollection.Parse("kern, liga"),
            Foreground = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Colors.MediumVioletRed, 0),
                    new GradientStop(Colors.DeepSkyBlue, 1)
                }
            }
        };

        control.Inlines!.Add(new Run("Rich render: "));
        control.Inlines.Add(new Bold { Inlines = { new Run("bold") } });
        control.Inlines.Add(new Run(", "));
        control.Inlines.Add(new Italic { Inlines = { new Run("italic") } });
        control.Inlines.Add(new Run(", decorated, spaced, gradient text stays in Pretext."));

        var window = new Window
        {
            Width = 500,
            Height = 180,
            Background = Brushes.White,
            Content = new Border
            {
                Padding = new Thickness(16),
                Child = control
            }
        };

        window.Show();

        var frame = window.CaptureRenderedFrame();
        Assert.False(control.IsUsingFallback);
        Assert.NotNull(frame);
    }

    [AvaloniaFact]
    public void Headless_scrollviewer_renders_dense_protextblocks_after_offset_change()
    {
        var panel = new StackPanel { Spacing = 6 };

        for (var i = 0; i < 80; i++)
        {
            panel.Children.Add(new ProTextBlockControl
            {
                Text = $"Row {i:00}: dense scrolling text stays clipped and repaintable.",
                Background = Brushes.White,
                Foreground = Brushes.DimGray,
                FontSize = 14,
                LineHeight = 20,
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 2
            });
        }

        var scrollViewer = new ScrollViewer
        {
            Content = panel,
            Background = Brushes.White
        };

        var window = new Window
        {
            Width = 420,
            Height = 260,
            Content = scrollViewer,
            Background = Brushes.White
        };

        window.Show();
        Assert.NotNull(window.CaptureRenderedFrame());

        scrollViewer.Offset = new Vector(0, 640);
        Assert.NotNull(window.CaptureRenderedFrame());
    }

    private static ProTextBlockControl CreateCacheProbe()
    {
        return new ProTextBlockControl
        {
            Text = "Shared cache probe text for repeated measurement.",
            FontSize = 16,
            LineHeight = 22,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black
        };
    }
}
