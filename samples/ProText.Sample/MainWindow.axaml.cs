using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ProTextCacheApi = ProText.ProTextCache;
using ProTextBlockControl = ProText.ProTextBlock;
using ProTextBoxControl = ProText.ProTextBox;
using ProTextPresenterControl = ProText.ProTextPresenter;

namespace ProText.Sample;

public partial class MainWindow : Window
{
    private const int TextBoxSurfaceItemCount = 1000;
    private const int TextBoxSurfaceColumns = 25;
    private const double TextBoxSurfacePadding = 90;
    private const double TextBoxSurfaceCellWidth = 370;
    private const double TextBoxSurfaceCellHeight = 102;
    private const double TextBoxSurfaceItemWidth = 300;
    private const double TextBoxSurfaceItemHeight = 48;
    private const double TextBoxSurfaceWheelZoomFactor = 1.12;
    private const double TextBoxSurfaceWheelPanStep = 72;

    private readonly List<TextBox> _avaloniaTextBoxes = [];
    private readonly List<ProTextBoxControl> _proTextBoxes = [];
    private readonly TextBoxSurfaceState _avaloniaTextBoxSurface = new();
    private readonly TextBoxSurfaceState _proTextBoxSurface = new();
    private TextBoxSurfaceState? _panningTextBoxSurface;
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
            ProTextCacheApi.Clear();
            UpdateCacheStatus();
        };

        AttachPanZoomSurface(AvaloniaTextBoxViewport, AvaloniaTextBoxCanvas, AvaloniaTextBoxZoomSlider, AvaloniaTextBoxZoomText, _avaloniaTextBoxSurface);
        AttachPanZoomSurface(ProTextBoxViewport, ProTextBoxCanvas, ProTextBoxZoomSlider, ProTextBoxZoomText, _proTextBoxSurface);
        AvaloniaTextBoxResetButton.Click += (_, _) => ResetTextBoxSurface(_avaloniaTextBoxSurface, AvaloniaTextBoxCanvas, AvaloniaTextBoxZoomSlider, AvaloniaTextBoxZoomText);
        ProTextBoxResetButton.Click += (_, _) => ResetTextBoxSurface(_proTextBoxSurface, ProTextBoxCanvas, ProTextBoxZoomSlider, ProTextBoxZoomText);

        CorpusBox.SelectedIndex = 1;
        RebuildDenseGrid();
        RebuildTextBoxSurfaces();
        ApplyTextBoxSurfaceTransform(AvaloniaTextBoxCanvas, AvaloniaTextBoxZoomSlider, AvaloniaTextBoxZoomText, _avaloniaTextBoxSurface);
        ApplyTextBoxSurfaceTransform(ProTextBoxCanvas, ProTextBoxZoomSlider, ProTextBoxZoomText, _proTextBoxSurface);
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
        var surfaceWidth = TextBoxSurfacePadding * 2 + (TextBoxSurfaceColumns - 1) * TextBoxSurfaceCellWidth + TextBoxSurfaceItemWidth;
        var surfaceHeight = TextBoxSurfacePadding * 2 + (rows - 1) * TextBoxSurfaceCellHeight + TextBoxSurfaceItemHeight;

        AvaloniaTextBoxCanvas.Width = surfaceWidth;
        AvaloniaTextBoxCanvas.Height = surfaceHeight;
        ProTextBoxCanvas.Width = surfaceWidth;
        ProTextBoxCanvas.Height = surfaceHeight;

        for (var i = 0; i < TextBoxSurfaceItemCount; i++)
        {
            var column = i % TextBoxSurfaceColumns;
            var row = i / TextBoxSurfaceColumns;
            var x = TextBoxSurfacePadding + column * TextBoxSurfaceCellWidth + TextBoxSurfaceJitter(i, 47, 92);
            var y = TextBoxSurfacePadding + row * TextBoxSurfaceCellHeight + TextBoxSurfaceJitter(i, 83, 36);
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

    private static void ApplyTextBoxSurfaceTransform(Canvas contentCanvas, Slider slider, TextBlock zoomText, TextBoxSurfaceState state)
    {
        state.Zoom = Math.Clamp(state.Zoom, slider.Minimum, slider.Maximum);
        slider.Value = state.Zoom;
        contentCanvas.RenderTransform = new MatrixTransform(new Matrix(state.Zoom, 0, 0, state.Zoom, state.Offset.X, state.Offset.Y));
        zoomText.Text = $"{state.Zoom * 100:0}%";
    }

    private static void ResetTextBoxSurface(TextBoxSurfaceState state, Canvas contentCanvas, Slider slider, TextBlock zoomText)
    {
        state.Zoom = 1;
        state.Offset = TextBoxSurfaceState.InitialOffset;
        ApplyTextBoxSurfaceTransform(contentCanvas, slider, zoomText, state);
    }

    private void AttachPanZoomSurface(Control viewport, Canvas contentCanvas, Slider slider, TextBlock zoomText, TextBoxSurfaceState state)
    {
        slider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
            {
                state.Zoom = slider.Value;
                ApplyTextBoxSurfaceTransform(contentCanvas, slider, zoomText, state);
            }
        };

        viewport.AddHandler<PointerWheelEventArgs>(
            InputElement.PointerWheelChangedEvent,
            (_, e) => WheelTextBoxSurface(e, viewport, contentCanvas, slider, zoomText, state),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        viewport.AddHandler<PointerPressedEventArgs>(
            InputElement.PointerPressedEvent,
            (_, e) => StartTextBoxSurfacePan(e, viewport, state),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        viewport.AddHandler<PointerEventArgs>(
            InputElement.PointerMovedEvent,
            (_, e) => MoveTextBoxSurfacePan(e, viewport, contentCanvas, slider, zoomText, state),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        viewport.AddHandler<PointerReleasedEventArgs>(
            InputElement.PointerReleasedEvent,
            (_, e) => StopTextBoxSurfacePan(e, state),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        viewport.PointerCaptureLost += (_, _) =>
        {
            if (ReferenceEquals(_panningTextBoxSurface, state))
            {
                _panningTextBoxSurface = null;
            }
        };
    }

    private static void WheelTextBoxSurface(PointerWheelEventArgs e, Control viewport, Canvas contentCanvas, Slider slider, TextBlock zoomText, TextBoxSurfaceState state)
    {
        var position = e.GetPosition(viewport);

        if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
        {
            state.Offset += new Vector(e.Delta.Y * TextBoxSurfaceWheelPanStep, e.Delta.X * TextBoxSurfaceWheelPanStep);
        }
        else if ((e.KeyModifiers & KeyModifiers.Alt) != 0)
        {
            state.Offset += new Vector(e.Delta.X * TextBoxSurfaceWheelPanStep, e.Delta.Y * TextBoxSurfaceWheelPanStep);
        }
        else
        {
            var oldZoom = state.Zoom;
            var zoomFactor = e.Delta.Y > 0 ? TextBoxSurfaceWheelZoomFactor : 1 / TextBoxSurfaceWheelZoomFactor;
            var newZoom = Math.Clamp(oldZoom * zoomFactor, slider.Minimum, slider.Maximum);
            var worldPoint = new Point((position.X - state.Offset.X) / oldZoom, (position.Y - state.Offset.Y) / oldZoom);

            state.Zoom = newZoom;
            state.Offset = new Vector(position.X - worldPoint.X * newZoom, position.Y - worldPoint.Y * newZoom);
        }

        ApplyTextBoxSurfaceTransform(contentCanvas, slider, zoomText, state);
        e.Handled = true;
    }

    private void StartTextBoxSurfacePan(PointerPressedEventArgs e, Control viewport, TextBoxSurfaceState state)
    {
        var point = e.GetCurrentPoint(viewport);

        if (!point.Properties.IsMiddleButtonPressed && !point.Properties.IsRightButtonPressed)
        {
            return;
        }

        _panningTextBoxSurface = state;
        state.PanStartPoint = point.Position;
        state.PanStartOffset = state.Offset;
        e.Pointer.Capture(viewport);
        e.Handled = true;
    }

    private void MoveTextBoxSurfacePan(PointerEventArgs e, Control viewport, Canvas contentCanvas, Slider slider, TextBlock zoomText, TextBoxSurfaceState state)
    {
        if (!ReferenceEquals(_panningTextBoxSurface, state))
        {
            return;
        }

        var position = e.GetPosition(viewport);
        var delta = position - state.PanStartPoint;
        state.Offset = state.PanStartOffset + delta;
        ApplyTextBoxSurfaceTransform(contentCanvas, slider, zoomText, state);
        e.Handled = true;
    }

    private void StopTextBoxSurfacePan(PointerReleasedEventArgs e, TextBoxSurfaceState state)
    {
        if (!ReferenceEquals(_panningTextBoxSurface, state))
        {
            return;
        }

        _panningTextBoxSurface = null;
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
        var snapshot = ProTextCacheApi.GetSnapshot();
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

    private static double TextBoxSurfaceJitter(int index, int multiplier, double range)
    {
        return (((index * multiplier) % 101) / 100.0 - 0.5) * range;
    }

    private sealed class TextBoxSurfaceState
    {
        public static readonly Vector InitialOffset = new(48, 48);

        public double Zoom { get; set; } = 1;

        public Vector Offset { get; set; } = InitialOffset;

        public Point PanStartPoint { get; set; }

        public Vector PanStartOffset { get; set; }
    }

    private sealed record CorpusItem(string Name, string Text)
    {
        public override string ToString() => Name;
    }
}