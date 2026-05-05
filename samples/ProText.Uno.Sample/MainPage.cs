using System.Reflection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace ProText.Uno.Sample;

public sealed class MainPage : Page
{
    private readonly IReadOnlyList<CorpusItem> _corpora =
    [
        new("Short labels", "Frame time, cache hits, visible rows, selected segment, Pretext path, measured width, logical line count."),
        new("Editorial paragraph", "Text layout is one of the most repeated operations in a desktop UI. A standard WinUI TextBlock asks the framework text stack to shape and arrange content whenever size or typography changes. ProTextBlock keeps the familiar text surface, but prepares text through PretextSharp, shares prepared segment data through a process-wide cache, and relayouts by width with reusable snapshots."),
        new("Dense operations", "Search results, telemetry rows, code annotations, chat previews, validation messages, and timeline labels all put pressure on the same path: thousands of short or medium text blocks measured repeatedly while the viewport changes."),
        new("Multilingual", "Hello world. Привет мир. مرحبا بالعالم. こんにちは世界。Text can contain punctuation, soft hyphen opportunities, non-breaking spaces, and scripts with different break behavior while staying on the ProText path.")
    ];

    private readonly ComboBox _corpusBox = new();
    private readonly Slider _fontSizeSlider = new() { Minimum = 11, Maximum = 28, Value = 16, StepFrequency = 1 };
    private readonly Slider _maxLinesSlider = new() { Minimum = 0, Maximum = 12, Value = 0, StepFrequency = 1 };
    private readonly CheckBox _wrapCheckBox = new() { Content = "Wrap text", IsChecked = true };
    private readonly CheckBox _cacheCheckBox = new() { Content = "Use global cache", IsChecked = true };
    private readonly TextBlock _fontSizeValueText = CreateCaption();
    private readonly TextBlock _maxLinesValueText = CreateCaption();
    private readonly TextBlock _cacheStatusText = CreateCaption();
    private readonly TextBlock _winUiText = CreateDisplayTextBlock();
    private readonly FrameworkElement _proText = ProTextUnoScaffold.CreateTextControl("ProTextBlock");
    private readonly TextBlock _winUiInlineText = CreateInlineTextBlock();
    private readonly FrameworkElement _proInlineText = ProTextUnoScaffold.CreateInlineTextControl("ProTextBlock");
    private readonly FrameworkElement _proPresenter = ProTextUnoScaffold.CreateTextControl("ProTextPresenter");
    private readonly FrameworkElement _proTextBox = ProTextUnoScaffold.CreateTextControl("ProTextBox");
    private readonly List<TextBlock> _winUiDenseTextBlocks = [];
    private readonly List<FrameworkElement> _proDenseTextBlocks = [];

    public MainPage()
    {
        Background = Brush(Colors.WhiteSmoke);
        Content = BuildContent();

        _corpusBox.ItemsSource = _corpora;
        _corpusBox.DisplayMemberPath = nameof(CorpusItem.Name);
        _corpusBox.SelectionChanged += (_, _) => UpdateContent();
        _fontSizeSlider.ValueChanged += (_, _) => UpdateContent();
        _maxLinesSlider.ValueChanged += (_, _) => UpdateContent();
        _wrapCheckBox.Click += (_, _) => UpdateContent();
        _cacheCheckBox.Click += (_, _) => UpdateContent();

        _corpusBox.SelectedIndex = 1;
        RebuildDenseRows();
        UpdateContent();
    }

    private UIElement BuildContent()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Border
        {
            Background = Brush(Colors.White),
            BorderBrush = Brush(Colors.Gainsboro),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(24, 18, 24, 18),
            Child = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children =
                {
                    new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            CreateTitle("ProText.Uno"),
                            CreateCaption("WinUI TextBlock-compatible comparison surfaces for ProText.Uno controls.")
                        }
                    },
                    _cacheStatusText
                }
            }
        };
        Grid.SetColumn(_cacheStatusText, 1);
        root.Children.Add(header);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        body.Children.Add(BuildSidebar());

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = BuildComparisonContent()
        };
        Grid.SetColumn(scrollViewer, 1);
        body.Children.Add(scrollViewer);

        return root;
    }

    private UIElement BuildSidebar()
    {
        var clearCacheButton = new Button
        {
            Content = "Clear cache",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        clearCacheButton.Click += (_, _) =>
        {
            ProTextUnoScaffold.ClearCache();
            UpdateCacheStatus();
        };

        return new Border
        {
            Background = Brush(Colors.GhostWhite),
            BorderBrush = Brush(Colors.Gainsboro),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(20),
            Child = new StackPanel
            {
                Spacing = 18,
                Children =
                {
                    CreateEditorGroup("Corpus", _corpusBox),
                    CreateEditorGroup("Font size", _fontSizeSlider, _fontSizeValueText),
                    CreateEditorGroup("Max lines", _maxLinesSlider, _maxLinesValueText),
                    new StackPanel
                    {
                        Spacing = 8,
                        Children =
                        {
                            _wrapCheckBox,
                            _cacheCheckBox
                        }
                    },
                    clearCacheButton
                }
            }
        };
    }

    private UIElement BuildComparisonContent()
    {
        var content = new StackPanel
        {
            Padding = new Thickness(24),
            Spacing = 18
        };

        content.Children.Add(CreateTwoColumnSection(
            "WinUI TextBlock",
            _winUiText,
            "ProText.Uno ProTextBlock",
            _proText));

        content.Children.Add(CreateTwoColumnSection(
            "WinUI inline TextBlock",
            _winUiInlineText,
            "ProText.Uno inline ProTextBlock",
            _proInlineText));

        content.Children.Add(CreateTwoColumnSection(
            "ProTextPresenter",
            _proPresenter,
            "ProTextBox",
            _proTextBox));

        var winUiDensePanel = new StackPanel { Spacing = 8 };
        var proDensePanel = new StackPanel { Spacing = 8 };
        content.Children.Add(CreateTwoColumnSection(
            "Dense WinUI TextBlocks",
            winUiDensePanel,
            "Dense ProTextBlocks",
            proDensePanel));

        for (var i = 0; i < 36; i++)
        {
            var text = $"Row {i + 1:00}: {DenseText(i)}";
            var winUiText = CreateDisplayTextBlock(text);
            var proText = ProTextUnoScaffold.CreateTextControl("ProTextBlock");

            _winUiDenseTextBlocks.Add(winUiText);
            _proDenseTextBlocks.Add(proText);
            winUiDensePanel.Children.Add(winUiText);
            proDensePanel.Children.Add(proText);
        }

        return content;
    }

    private static Border CreateTwoColumnSection(string leftTitle, UIElement leftContent, string rightTitle, UIElement rightContent)
    {
        var grid = new Grid { ColumnSpacing = 18 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var leftPanel = CreatePanel(leftTitle, leftContent);
        var rightPanel = CreatePanel(rightTitle, rightContent);
        Grid.SetColumn(rightPanel, 1);
        grid.Children.Add(leftPanel);
        grid.Children.Add(rightPanel);

        return new Border
        {
            Child = grid
        };
    }

    private static Border CreatePanel(string title, UIElement content)
    {
        return new Border
        {
            Background = Brush(Colors.White),
            BorderBrush = Brush(Colors.Gainsboro),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(18),
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    CreateSectionTitle(title),
                    content
                }
            }
        };
    }

    private static StackPanel CreateEditorGroup(string title, params UIElement[] controls)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(CreateSectionTitle(title));

        foreach (var control in controls)
        {
            panel.Children.Add(control);
        }

        return panel;
    }

    private void RebuildDenseRows()
    {
        for (var i = 0; i < _winUiDenseTextBlocks.Count; i++)
        {
            var text = $"Row {i + 1:00}: {DenseText(i)}";
            _winUiDenseTextBlocks[i].Text = text;
            ProTextUnoScaffold.ApplyText(_proDenseTextBlocks[i], text);
        }
    }

    private void UpdateContent()
    {
        var corpus = _corpusBox.SelectedItem as CorpusItem ?? _corpora[1];
        var fontSize = Math.Round(_fontSizeSlider.Value);
        var maxLines = (int)Math.Round(_maxLinesSlider.Value);
        var wrapping = _wrapCheckBox.IsChecked == true ? TextWrapping.Wrap : TextWrapping.NoWrap;
        var lineHeight = Math.Round(fontSize * 1.42);
        var useGlobalCache = _cacheCheckBox.IsChecked == true;

        _fontSizeValueText.Text = $"{fontSize:0} px";
        _maxLinesValueText.Text = maxLines == 0 ? "Unlimited" : maxLines.ToString();

        ApplyTextBlockSettings(_winUiText, corpus.Text, fontSize, lineHeight, wrapping, maxLines);
        ApplyTextBlockStyle(_winUiInlineText, fontSize, lineHeight, wrapping, maxLines);
        ProTextUnoScaffold.ApplyTextSettings(_proText, corpus.Text, fontSize, lineHeight, wrapping, maxLines, useGlobalCache);
        ProTextUnoScaffold.ApplyStyleSettings(_proInlineText, fontSize, lineHeight, wrapping, maxLines, useGlobalCache);
        ProTextUnoScaffold.ApplyTextSettings(_proPresenter, "Presenter selection, caret, preedit, password masking, and rich inline presentation share the ProText layout core.", fontSize, lineHeight, wrapping, maxLines, useGlobalCache);
        ProTextUnoScaffold.ApplyTextSettings(_proTextBox, corpus.Text, fontSize, lineHeight, wrapping, maxLines, useGlobalCache);

        for (var i = 0; i < _winUiDenseTextBlocks.Count; i++)
        {
            var text = $"Row {i + 1:00}: {DenseText(i)}";
            ApplyTextBlockSettings(_winUiDenseTextBlocks[i], text, Math.Max(11, fontSize - 2), Math.Round(Math.Max(11, fontSize - 2) * 1.35), wrapping, 2);
            ProTextUnoScaffold.ApplyTextSettings(_proDenseTextBlocks[i], text, Math.Max(11, fontSize - 2), Math.Round(Math.Max(11, fontSize - 2) * 1.35), wrapping, 2, useGlobalCache);
        }

        UpdateCacheStatus();
    }

    private void UpdateCacheStatus()
    {
        _cacheStatusText.Text = ProTextUnoScaffold.GetCacheStatus();
    }

    private static void ApplyTextBlockSettings(TextBlock textBlock, string text, double fontSize, double lineHeight, TextWrapping wrapping, int maxLines)
    {
        textBlock.Text = text;
        ApplyTextBlockStyle(textBlock, fontSize, lineHeight, wrapping, maxLines);
    }

    private static void ApplyTextBlockStyle(TextBlock textBlock, double fontSize, double lineHeight, TextWrapping wrapping, int maxLines)
    {
        textBlock.FontSize = fontSize;
        textBlock.LineHeight = lineHeight;
        textBlock.MaxLines = maxLines;
        textBlock.TextWrapping = wrapping;
    }

    private static TextBlock CreateDisplayTextBlock(string text = "")
    {
        return new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush(Colors.Black)
        };
    }

    private static TextBlock CreateInlineTextBlock()
    {
        var textBlock = CreateDisplayTextBlock();
        textBlock.Inlines.Add(new Run { Text = "Inline content: " });
        textBlock.Inlines.Add(new Bold { Inlines = { new Run { Text = "bold" } } });
        textBlock.Inlines.Add(new Run { Text = ", " });
        textBlock.Inlines.Add(new Italic { Inlines = { new Run { Text = "italic" } } });
        textBlock.Inlines.Add(new Run { Text = ", and " });
        textBlock.Inlines.Add(new Underline { Inlines = { new Run { Text = "underlined" } } });
        textBlock.Inlines.Add(new Run { Text = " runs use matching inline styling in both comparison columns." });
        return textBlock;
    }

    private static TextBlock CreateTitle(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 24,
            Foreground = Brush(Colors.Black)
        };
    }

    private static TextBlock CreateSectionTitle(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 13,
            Foreground = Brush(Colors.DarkSlateGray)
        };
    }

    private static TextBlock CreateCaption(string text = "")
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 12,
            Foreground = Brush(Colors.DimGray),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static SolidColorBrush Brush(Windows.UI.Color color)
    {
        return new SolidColorBrush(color);
    }

    private static string DenseText(int index)
    {
        return (index % 4) switch
        {
            0 => "cache hit, wrapped label, two measured lines",
            1 => "localized preview: Hello, Привет, مرحبا, こんにちは",
            2 => "rich run intent with bold, italic, underline markers",
            _ => "scrolling surface row for repeated text measurement"
        };
    }

    private sealed record CorpusItem(string Name, string Text);
}

internal static class ProTextUnoScaffold
{
    private const string AssemblyName = "ProText.Uno";

    public static FrameworkElement CreateTextControl(string controlName)
    {
        if (TryCreateControl(controlName) is { } control)
        {
            return control;
        }

        return CreatePendingText(controlName);
    }

    public static FrameworkElement CreateInlineTextControl(string controlName)
    {
        if (TryCreateControl(controlName) is { } control)
        {
            AddInlineRuns(control);
            return control;
        }

        var fallback = CreatePendingText(controlName);
        fallback.Text = "Inline content: bold, italic, and underlined runs will bind to ProText.Uno when src/ProText.Uno is present.";
        return fallback;
    }

    public static void ApplyText(FrameworkElement element, string text)
    {
        if (element is TextBlock textBlock)
        {
            textBlock.Text = text;
            return;
        }

        SetProperty(element, "Text", text);
    }

    public static void ApplyTextSettings(FrameworkElement element, string text, double fontSize, double lineHeight, TextWrapping wrapping, int maxLines, bool useGlobalCache)
    {
        if (element is TextBlock textBlock)
        {
            textBlock.Text = text;
            ApplyTextBlockStyle(textBlock, fontSize, lineHeight, wrapping, maxLines);
            return;
        }

        SetProperty(element, "Text", text);
        ApplyStyleSettings(element, fontSize, lineHeight, wrapping, maxLines, useGlobalCache);
    }

    public static void ApplyStyleSettings(FrameworkElement element, double fontSize, double lineHeight, TextWrapping wrapping, int maxLines, bool useGlobalCache)
    {
        if (element is TextBlock textBlock)
        {
            ApplyTextBlockStyle(textBlock, fontSize, lineHeight, wrapping, maxLines);
            return;
        }

        SetProperty(element, "FontSize", fontSize);
        SetProperty(element, "LineHeight", lineHeight);
        SetProperty(element, "TextWrapping", wrapping);
        SetProperty(element, "MaxLines", maxLines);
        SetProperty(element, "UseGlobalCache", useGlobalCache);
        SetProperty(element, "Foreground", new SolidColorBrush(Colors.Black));
        SetProperty(element, "AcceptsReturn", true);
    }

    private static void ApplyTextBlockStyle(TextBlock textBlock, double fontSize, double lineHeight, TextWrapping wrapping, int maxLines)
    {
        textBlock.FontSize = fontSize;
        textBlock.LineHeight = lineHeight;
        textBlock.TextWrapping = wrapping;
        textBlock.MaxLines = maxLines;
    }

    public static void ClearCache()
    {
        InvokeCacheMethod("Clear");
    }

    public static string GetCacheStatus()
    {
        var snapshot = InvokeCacheMethod("GetSnapshot");
        if (snapshot is null)
        {
            return "ProText.Uno unavailable";
        }

        var count = ReadProperty(snapshot, "Count");
        var hits = ReadProperty(snapshot, "Hits");
        var misses = ReadProperty(snapshot, "Misses");
        return $"Cache entries {count}, hits {hits}, misses {misses}";
    }

    private static FrameworkElement? TryCreateControl(string controlName)
    {
        var type = ResolveType(controlName);
        return type is null ? null : Activator.CreateInstance(type) as FrameworkElement;
    }

    private static Type? ResolveType(string controlName)
    {
        return Type.GetType($"{AssemblyName}.{controlName}, {AssemblyName}", throwOnError: false);
    }

    private static TextBlock CreatePendingText(string controlName)
    {
        return new TextBlock
        {
            Text = $"{controlName} unavailable: build with the ProText.Uno project reference to activate this column.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Colors.DimGray)
        };
    }

    private static void AddInlineRuns(FrameworkElement control)
    {
        var inlines = control.GetType().GetProperty("Inlines", BindingFlags.Instance | BindingFlags.Public)?.GetValue(control);
        var addMethod = inlines?.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
        if (addMethod is null)
        {
            SetProperty(control, "Text", "Inline content: bold, italic, and underlined runs use the ProText rich inline path.");
            return;
        }

        addMethod.Invoke(inlines, [new Run { Text = "Inline content: " }]);
        addMethod.Invoke(inlines, [new Bold { Inlines = { new Run { Text = "bold" } } }]);
        addMethod.Invoke(inlines, [new Run { Text = ", " }]);
        addMethod.Invoke(inlines, [new Italic { Inlines = { new Run { Text = "italic" } } }]);
        addMethod.Invoke(inlines, [new Run { Text = ", and " }]);
        addMethod.Invoke(inlines, [new Underline { Inlines = { new Run { Text = "underlined" } } }]);
        addMethod.Invoke(inlines, [new Run { Text = " runs use matching inline styling in both comparison columns." }]);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is { CanWrite: true } && property.PropertyType.IsInstanceOfType(value))
        {
            property.SetValue(target, value);
        }
    }

    private static object? InvokeCacheMethod(string methodName)
    {
        var cacheType = ResolveType("ProTextCache");
        return cacheType?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
    }

    private static object? ReadProperty(object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target);
    }
}
