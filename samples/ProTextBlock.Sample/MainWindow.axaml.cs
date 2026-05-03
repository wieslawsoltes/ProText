using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using ProTextBlockCacheApi = ProTextBlock.ProTextBlockCache;
using ProTextBlockControl = ProTextBlock.ProTextBlock;

namespace ProTextBlock.Sample;

public partial class MainWindow : Window
{
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

        CorpusBox.SelectedIndex = 1;
        RebuildDenseGrid();
        UpdateContent();
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

    private sealed record CorpusItem(string Name, string Text)
    {
        public override string ToString() => Name;
    }
}