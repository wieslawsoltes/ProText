using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ProTextBlockCacheApi = ProTextBlock.ProTextBlockCache;
using ProTextBlockControl = ProTextBlock.ProTextBlock;
using ProTextBoxControl = ProTextBlock.ProTextBox;
using ProTextPresenterControl = ProTextBlock.ProTextPresenter;

namespace ProTextBlock.Sample;

public partial class MainWindow : Window
{
    private const int TextBoxSurfaceItemCount = 1000;
    private const int TextBoxSurfaceColumns = 4;
    private const double TextBoxSurfacePadding = 20;
    private const double TextBoxSurfaceItemWidth = 360;
    private const double TextBoxSurfaceItemHeight = 52;
    private const double TextBoxSurfaceColumnGap = 16;
    private const double TextBoxSurfaceRowGap = 12;

    private readonly List<TextBox> _avaloniaTextBoxes = [];
    private readonly List<ProTextBoxControl> _proTextBoxes = [];
    private ScrollViewer? _panningScrollViewer;
    private Point _panStartPoint;
    private Vector _panStartOffset;
    private readonly IReadOnlyList<CorpusItem> _corpora =
    [
        new("Short labels", "Frame time, cache hits, visible rows, selected segment, Pretext path, measured width, logical line count."),
        new("Editorial paragraph", "Text layout is one of the most repeated operations in a desktop UI. A normal TextBlock asks the framework text stack to shape and arrange content whenever size or typography changes. ProTextBlock keeps Avalonia's public surface familiar, but prepares simple text through PretextSharp, shares prepared segment data through a process-wide cache, and relayouts by width with arithmetic instead of rebuilding the full text layout object. Resize this window and watch both columns stay synchronized."),
        new("Dense operations", "Search results, telemetry rows, code annotations, chat previews, validation messages, and timeline labels all put pressure on the same path: thousands of short or medium text blocks measured repeatedly while the viewport changes. The global cache keeps identical text and font descriptors hot across controls while each ProTextBlock keeps width-specific layout data local."),
        new("Multilingual", "Hello world. Привет мир. مرحبا بالعالم. こんにちは世界。Text can contain punctuation, soft hyphen opportunities, non-breaking spaces, and scripts with different break behavior while still flowing through the same prepared layout pipeline.")
    ];

    public MainWindow()
    {
        InitializeComponent();

        CorpusBox.ItemsSource = _corpora;
        CorpusBox.SelectionChanged += (_, _) => UpdateContent();
        FontSizeSlider.PropertyChanged += (_, e) => { if (e.Property == Slider.ValueProperty) UpdateContent(); };
        MaxLinesSlider.PropertyChanged += (_, e) => { if (e.Property == Slider.ValueProperty) UpdateContent(); };
        WrapCheckBox.IsCheckedChanged += (_, _) => UpdateContent();
        CacheCheckBox.IsCheckedChanged += (_, _) => UpdateContent();
        ClearCacheButton.Click += (_, _) =>
        {
            ProTextBlockCacheApi.Clear();
            UpdateCacheStatus();
        };

        AvaloniaTextBoxZoomSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
            {
                ApplyTextBoxSurfaceZoom(AvaloniaTextBoxZoomSlider, AvaloniaTextBoxZoomSurface, AvaloniaTextBoxCanvas, AvaloniaTextBoxZoomText);
            }
        };
        ProTextBoxZoomSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
            {
                ApplyTextBoxSurfaceZoom(ProTextBoxZoomSlider, ProTextBoxZoomSurface, ProTextBoxCanvas, ProTextBoxZoomText);
            }
        };
        AvaloniaTextBoxResetButton.Click += (_, _) => ResetTextBoxSurface(AvaloniaTextBoxZoomSlider, AvaloniaTextBoxPanScrollViewer);
        ProTextBoxResetButton.Click += (_, _) => ResetTextBoxSurface(ProTextBoxZoomSlider, ProTextBoxPanScrollViewer);
        AttachPanZoomSurface(AvaloniaTextBoxPanScrollViewer, AvaloniaTextBoxZoomSlider);
        AttachPanZoomSurface(ProTextBoxPanScrollViewer, ProTextBoxZoomSlider);

        CorpusBox.SelectedIndex = 1;
        RebuildDenseGrid();
        RebuildTextBoxSurfaces();
        ApplyTextBoxSurfaceZoom(AvaloniaTextBoxZoomSlider, AvaloniaTextBoxZoomSurface, AvaloniaTextBoxCanvas, AvaloniaTextBoxZoomText);
        ApplyTextBoxSurfaceZoom(ProTextBoxZoomSlider, ProTextBoxZoomSurface, ProTextBoxCanvas, ProTextBoxZoomText);
        UpdateContent();
        PresenterDemo.ShowCaret();
    }

    private void UpdateContent()
    {
        var corpus = CorpusBox.SelectedItem as CorpusItem ?? _corpora[1];
        var fontSize = Math.Round(FontSizeSlider.Value);
        var maxLines = (int)Math.Round(MaxLinesSlider.Value);
        var wrapping = WrapCheckBox.IsChecked == true ? TextWrapping.Wrap : TextWrapping.NoWrap;
        var lineHeight = Math.Round(fontSize * 1.42);
        var useGlobalCache = CacheCheckBox.IsChecked == true;

        FontSizeValueText.Text = $"{fontSize:0} px";
        MaxLinesValueText.Text = maxLines == 0 ? "Unlimited" : maxLines.ToString();

        AvaloniaText.Text = corpus.Text;
        AvaloniaText.FontSize = fontSize;
        AvaloniaText.LineHeight = lineHeight;
        AvaloniaText.MaxLines = maxLines;
        AvaloniaText.TextWrapping = wrapping;

        ProText.Text = corpus.Text;
        ProText.FontSize = fontSize;
        ProText.LineHeight = lineHeight;
        ProText.MaxLines = maxLines;
        ProText.TextWrapping = wrapping;
        ProText.UseGlobalCache = useGlobalCache;

        ApplyPresenterText(PresenterDemo, fontSize, wrapping, useGlobalCache);
        ApplyPresenterText(PresenterInlineDemo, fontSize, wrapping, useGlobalCache);
        ApplyEditableText(corpus.Text, fontSize, wrapping, useGlobalCache);
        ApplyTextBoxSurfaceSettings(fontSize, wrapping, useGlobalCache);

        foreach (var child in AvaloniaDenseGrid.Children)
        {
            if (child is TextBlock textBlock)
            {
                ApplyDenseText(textBlock, fontSize, wrapping);
            }
        }

        foreach (var child in ProDenseGrid.Children)
        {
            if (child is ProTextBlockControl textBlock)
            {
                textBlock.UseGlobalCache = useGlobalCache;
                ApplyDenseText(textBlock, fontSize, wrapping);
            }
        }

        UpdateCacheStatus();
    }

    private void RebuildDenseGrid()
    {
        AvaloniaDenseGrid.Children.Clear();
        ProDenseGrid.Children.Clear();

        for (var i = 0; i < 72; i++)
        {
            var text = $"Row {i + 1:00}: {DenseText(i)}";

            AvaloniaDenseGrid.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = Brushes.DimGray,
                Margin = new Avalonia.Thickness(0, 0, 10, 8),
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 2
            });

            ProDenseGrid.Children.Add(new ProTextBlockControl
            {
                Text = text,
                Background = Brushes.White,
                Foreground = Brushes.DimGray,
                Margin = new Avalonia.Thickness(0, 0, 10, 8),
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 2
            });
        }
    }

    private void RebuildTextBoxSurfaces()
    {
        _avaloniaTextBoxes.Clear();
        _proTextBoxes.Clear();
        AvaloniaTextBoxCanvas.Children.Clear();
        ProTextBoxCanvas.Children.Clear();

        var rows = (int)Math.Ceiling(TextBoxSurfaceItemCount / (double)TextBoxSurfaceColumns);
        var surfaceWidth = TextBoxSurfacePadding * 2 + TextBoxSurfaceColumns * TextBoxSurfaceItemWidth + (TextBoxSurfaceColumns - 1) * TextBoxSurfaceColumnGap;
        var surfaceHeight = TextBoxSurfacePadding * 2 + rows * TextBoxSurfaceItemHeight + (rows - 1) * TextBoxSurfaceRowGap;

        AvaloniaTextBoxCanvas.Width = surfaceWidth;
        AvaloniaTextBoxCanvas.Height = surfaceHeight;
        ProTextBoxCanvas.Width = surfaceWidth;
        ProTextBoxCanvas.Height = surfaceHeight;

        for (var i = 0; i < TextBoxSurfaceItemCount; i++)
        {
            var column = i % TextBoxSurfaceColumns;
            var row = i / TextBoxSurfaceColumns;
            var x = TextBoxSurfacePadding + column * (TextBoxSurfaceItemWidth + TextBoxSurfaceColumnGap);
            var y = TextBoxSurfacePadding + row * (TextBoxSurfaceItemHeight + TextBoxSurfaceRowGap);
            var text = TextBoxSurfaceText(i);

            var avaloniaTextBox = new TextBox
            {
                Text = text,
                Width = TextBoxSurfaceItemWidth,
                Height = TextBoxSurfaceItemHeight,
                FontSize = 14,
                LineHeight = 20,
                TextWrapping = TextWrapping.Wrap,
                SelectionBrush = new SolidColorBrush(Color.FromArgb(96, 59, 130, 246)),
                SelectionForegroundBrush = Brushes.White,
                CaretBrush = Brushes.Black
            };
            Canvas.SetLeft(avaloniaTextBox, x);
            Canvas.SetTop(avaloniaTextBox, y);
            AvaloniaTextBoxCanvas.Children.Add(avaloniaTextBox);
            _avaloniaTextBoxes.Add(avaloniaTextBox);

            var proTextBox = new ProTextBoxControl
            {
                Text = text,
                Width = TextBoxSurfaceItemWidth,
                Height = TextBoxSurfaceItemHeight,
                FontSize = 14,
                LineHeight = 20,
                TextWrapping = TextWrapping.Wrap,
                SelectionBrush = new SolidColorBrush(Color.FromArgb(96, 59, 130, 246)),
                SelectionForegroundBrush = Brushes.White,
                CaretBrush = Brushes.Black,
                UseGlobalCache = true
            };
            Canvas.SetLeft(proTextBox, x);
            Canvas.SetTop(proTextBox, y);
            ProTextBoxCanvas.Children.Add(proTextBox);
            _proTextBoxes.Add(proTextBox);
        }
    }

    private void ApplyTextBoxSurfaceSettings(double fontSize, TextWrapping wrapping, bool useGlobalCache)
    {
        var surfaceFontSize = Math.Max(11, fontSize - 2);
        var lineHeight = Math.Round(surfaceFontSize * 1.35);

        foreach (var textBox in _avaloniaTextBoxes)
        {
            textBox.FontSize = surfaceFontSize;
            textBox.LineHeight = lineHeight;
            textBox.TextWrapping = wrapping;
        }

        foreach (var textBox in _proTextBoxes)
        {
            textBox.FontSize = surfaceFontSize;
            textBox.LineHeight = lineHeight;
            textBox.TextWrapping = wrapping;
            textBox.UseGlobalCache = useGlobalCache;
        }
    }

    private static void ApplyTextBoxSurfaceZoom(Slider slider, Canvas zoomSurface, Canvas contentCanvas, TextBlock zoomText)
    {
        var zoom = Math.Clamp(slider.Value, slider.Minimum, slider.Maximum);
        zoomSurface.Width = contentCanvas.Width * zoom;
        zoomSurface.Height = contentCanvas.Height * zoom;
        contentCanvas.RenderTransform = new ScaleTransform(zoom, zoom);
        zoomText.Text = $"{zoom * 100:0}%";
    }

    private static void ResetTextBoxSurface(Slider slider, ScrollViewer scrollViewer)
    {
        slider.Value = 1;
        scrollViewer.Offset = default;
    }

    private static void ZoomTextBoxSurface(PointerWheelEventArgs e, Slider slider)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) == 0)
        {
            return;
        }

        var step = e.Delta.Y > 0 ? 0.1 : -0.1;
        slider.Value = Math.Clamp(slider.Value + step, slider.Minimum, slider.Maximum);
        e.Handled = true;
    }

    private void AttachPanZoomSurface(ScrollViewer scrollViewer, Slider slider)
    {
        scrollViewer.PointerWheelChanged += (_, e) => ZoomTextBoxSurface(e, slider);
        scrollViewer.PointerPressed += (_, e) => StartTextBoxSurfacePan(e, scrollViewer);
        scrollViewer.PointerMoved += (_, e) => MoveTextBoxSurfacePan(e, scrollViewer);
        scrollViewer.PointerReleased += (_, e) => StopTextBoxSurfacePan(e, scrollViewer);
        scrollViewer.PointerCaptureLost += (_, _) =>
        {
            if (ReferenceEquals(_panningScrollViewer, scrollViewer))
            {
                _panningScrollViewer = null;
            }
        };
    }

    private void StartTextBoxSurfacePan(PointerPressedEventArgs e, ScrollViewer scrollViewer)
    {
        var point = e.GetCurrentPoint(scrollViewer);

        if (!point.Properties.IsMiddleButtonPressed && !point.Properties.IsRightButtonPressed)
        {
            return;
        }

        _panningScrollViewer = scrollViewer;
        _panStartPoint = point.Position;
        _panStartOffset = scrollViewer.Offset;
        e.Pointer.Capture(scrollViewer);
        e.Handled = true;
    }

    private void MoveTextBoxSurfacePan(PointerEventArgs e, ScrollViewer scrollViewer)
    {
        if (!ReferenceEquals(_panningScrollViewer, scrollViewer))
        {
            return;
        }

        var position = e.GetPosition(scrollViewer);
        var delta = position - _panStartPoint;
        scrollViewer.Offset = new Vector(
            Math.Max(0, _panStartOffset.X - delta.X),
            Math.Max(0, _panStartOffset.Y - delta.Y));
        e.Handled = true;
    }

    private void StopTextBoxSurfacePan(PointerReleasedEventArgs e, ScrollViewer scrollViewer)
    {
        if (!ReferenceEquals(_panningScrollViewer, scrollViewer))
        {
            return;
        }

        _panningScrollViewer = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private static void ApplyDenseText(TextBlock textBlock, double fontSize, TextWrapping wrapping)
    {
        textBlock.FontSize = Math.Max(11, fontSize - 3);
        textBlock.LineHeight = Math.Round(textBlock.FontSize * 1.35);
        textBlock.TextWrapping = wrapping;
    }

    private static void ApplyDenseText(ProTextBlockControl textBlock, double fontSize, TextWrapping wrapping)
    {
        textBlock.FontSize = Math.Max(11, fontSize - 3);
        textBlock.LineHeight = Math.Round(textBlock.FontSize * 1.35);
        textBlock.TextWrapping = wrapping;
    }

    private static void ApplyPresenterText(ProTextPresenterControl presenter, double fontSize, TextWrapping wrapping, bool useGlobalCache)
    {
        presenter.FontSize = fontSize;
        presenter.LineHeight = Math.Round(fontSize * 1.42);
        presenter.TextWrapping = wrapping;
        presenter.UseGlobalCache = useGlobalCache;
    }

    private void ApplyEditableText(string text, double fontSize, TextWrapping wrapping, bool useGlobalCache)
    {
        var selectedEnd = Math.Min(text.Length, 120);

        AvaloniaEditBox.Text = text;
        AvaloniaEditBox.FontSize = fontSize;
        AvaloniaEditBox.LineHeight = Math.Round(fontSize * 1.42);
        AvaloniaEditBox.TextWrapping = wrapping;
        AvaloniaEditBox.SelectionStart = Math.Min(16, selectedEnd);
        AvaloniaEditBox.SelectionEnd = selectedEnd;
        AvaloniaEditBox.CaretIndex = selectedEnd;

        ApplyProTextBox(ProEditBox, text, fontSize, wrapping, useGlobalCache);
        ProEditBox.SelectionStart = Math.Min(16, selectedEnd);
        ProEditBox.SelectionEnd = selectedEnd;
        ProEditBox.CaretIndex = selectedEnd;

        ApplyProTextBox(ProPlaceholderBox, string.Empty, fontSize, wrapping, useGlobalCache);
        ApplyProTextBox(ProPasswordBox, "pretext-password", fontSize, TextWrapping.NoWrap, useGlobalCache);
        ProPasswordBox.CaretIndex = ProPasswordBox.Text?.Length ?? 0;
    }

    private static void ApplyProTextBox(ProTextBoxControl textBox, string text, double fontSize, TextWrapping wrapping, bool useGlobalCache)
    {
        textBox.Text = text;
        textBox.FontSize = fontSize;
        textBox.LineHeight = Math.Round(fontSize * 1.42);
        textBox.TextWrapping = wrapping;
        textBox.UseGlobalCache = useGlobalCache;
    }

    private void UpdateCacheStatus()
    {
        var snapshot = ProTextBlockCacheApi.GetSnapshot();
        CacheStatusText.Text = $"Cache entries {snapshot.Count} / {snapshot.MaxEntryCount}    Hits {snapshot.Hits}    Misses {snapshot.Misses}";
    }

    private static string DenseText(int index)
    {
        return (index % 6) switch
        {
            0 => "validation latency dropped after cached segment reuse",
            1 => "viewport resize uses the prepared text again",
            2 => "same label appears across grouped result rows",
            3 => "wrapping probes stay local to the control width",
            4 => "simple text uses the Pretext layout path",
            _ => "rich inline content stays on the Pretext path"
        };
    }

    private static string TextBoxSurfaceText(int index)
    {
        return $"Box {index + 1:0000}: {DenseText(index)} across a zoomable editable surface.";
    }

    private sealed record CorpusItem(string Name, string Text)
    {
        public override string ToString() => Name;
    }
}