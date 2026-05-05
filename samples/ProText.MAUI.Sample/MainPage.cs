using System.Reflection;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Hosting;

namespace ProText.MAUI.Sample;

public sealed class MainPage : ContentPage
{
    private readonly List<CorpusItem> _corpora =
    [
        new("Short labels", "Frame time, cache hits, visible rows, selected segment, Pretext path, measured width, logical line count."),
        new("Editorial paragraph", "Text layout is one of the most repeated operations in a cross-platform UI. A standard MAUI Label asks the framework text stack to shape and arrange content whenever size or typography changes. ProTextBlock keeps the familiar text surface, but prepares text through PretextSharp, shares prepared segment data through a process-wide cache, and relayouts by width with reusable snapshots."),
        new("Dense operations", "Search results, telemetry rows, code annotations, chat previews, validation messages, and timeline labels all put pressure on the same path: thousands of short or medium text blocks measured repeatedly while the viewport changes."),
        new("Multilingual", "Hello world. Привет мир. مرحبا بالعالم. こんにちは世界。Text can contain punctuation, soft hyphen opportunities, non-breaking spaces, and scripts with different break behavior while staying on the ProText path.")
    ];

    private readonly Picker _corpusPicker = new() { Title = "Corpus" };
    private readonly Slider _fontSizeSlider = new() { Minimum = 11, Maximum = 28, Value = 16 };
    private readonly Slider _maxLinesSlider = new() { Minimum = 0, Maximum = 12, Value = 0 };
    private readonly CheckBox _wrapCheckBox = new() { IsChecked = true };
    private readonly CheckBox _cacheCheckBox = new() { IsChecked = true };
    private readonly Label _fontSizeValueLabel = CreateCaption();
    private readonly Label _maxLinesValueLabel = CreateCaption();
    private readonly Label _cacheStatusLabel = CreateCaption();
    private readonly Label _mauiLabel = CreateDisplayLabel();
    private readonly View _proText = ProTextMauiScaffold.CreateTextControl("ProTextBlock");
    private readonly Label _mauiInlineLabel = CreateInlineLabel();
    private readonly View _proInlineText = ProTextMauiScaffold.CreateInlineTextControl("ProTextBlock");
    private readonly View _proPresenter = ProTextMauiScaffold.CreateTextControl("ProTextPresenter");
    private readonly Editor _mauiEditor = CreateEditor();
    private readonly View _proTextBox = ProTextMauiScaffold.CreateTextControl("ProTextBox");
    private readonly View _proEditorTextBox = ProTextMauiScaffold.CreateTextControl("ProTextBox");
    private readonly List<Label> _mauiDenseLabels = [];
    private readonly List<View> _proDenseTextBlocks = [];

    public MainPage()
    {
        Title = "ProText.MAUI";
        BackgroundColor = Colors.WhiteSmoke;
        Content = BuildContent();

        _corpusPicker.ItemsSource = _corpora;
        _corpusPicker.ItemDisplayBinding = new Binding(nameof(CorpusItem.Name));
        _corpusPicker.SelectedIndexChanged += (_, _) => UpdateContent();
        _fontSizeSlider.ValueChanged += (_, _) => UpdateContent();
        _maxLinesSlider.ValueChanged += (_, _) => UpdateContent();
        _wrapCheckBox.CheckedChanged += (_, _) => UpdateContent();
        _cacheCheckBox.CheckedChanged += (_, _) => UpdateContent();

        _corpusPicker.SelectedIndex = 1;
        RebuildDenseRows();
        UpdateContent();
    }

    private View BuildContent()
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 16
        };
        headerGrid.Children.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                CreateTitle("ProText.MAUI"),
                CreateCaption("MAUI Label and Editor comparison surfaces for ProText.MAUI controls.")
            }
        });
        Grid.SetColumn(_cacheStatusLabel, 1);
        headerGrid.Children.Add(_cacheStatusLabel);

        root.Children.Add(new Border
        {
            BackgroundColor = Colors.White,
            Stroke = new SolidColorBrush(Colors.Gainsboro),
            StrokeThickness = 0,
            Padding = new Thickness(24, 18),
            Content = headerGrid
        });

        var body = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(300) },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        body.Children.Add(BuildSidebar());

        var scrollView = new ScrollView
        {
            Content = BuildComparisonContent()
        };
        Grid.SetColumn(scrollView, 1);
        body.Children.Add(scrollView);

        return root;
    }

    private View BuildSidebar()
    {
        var clearCacheButton = new Button
        {
            Text = "Clear cache",
            HorizontalOptions = LayoutOptions.Fill
        };
        clearCacheButton.Clicked += (_, _) =>
        {
            ProTextMauiScaffold.ClearCache();
            UpdateCacheStatus();
        };

        return new Border
        {
            BackgroundColor = Colors.GhostWhite,
            Stroke = new SolidColorBrush(Colors.Gainsboro),
            StrokeThickness = 1,
            Padding = new Thickness(20),
            Content = new VerticalStackLayout
            {
                Spacing = 18,
                Children =
                {
                    CreateEditorGroup("Corpus", _corpusPicker),
                    CreateEditorGroup("Font size", _fontSizeSlider, _fontSizeValueLabel),
                    CreateEditorGroup("Max lines", _maxLinesSlider, _maxLinesValueLabel),
                    CreateCheckRow(_wrapCheckBox, "Wrap text"),
                    CreateCheckRow(_cacheCheckBox, "Use global cache"),
                    clearCacheButton
                }
            }
        };
    }

    private View BuildComparisonContent()
    {
        var content = new VerticalStackLayout
        {
            Padding = new Thickness(24),
            Spacing = 18
        };

        content.Children.Add(CreateTwoColumnSection(
            "MAUI Label",
            _mauiLabel,
            "ProText.MAUI ProTextBlock",
            _proText));

        content.Children.Add(CreateTwoColumnSection(
            "MAUI formatted Label",
            _mauiInlineLabel,
            "ProText.MAUI formatted ProTextBlock",
            _proInlineText));

        content.Children.Add(CreateTwoColumnSection(
            "ProTextPresenter",
            _proPresenter,
            "ProTextBox",
            _proTextBox));

        content.Children.Add(CreateTwoColumnSection(
            "MAUI Editor",
            _mauiEditor,
            "ProText.MAUI ProTextBox",
            _proEditorTextBox));

        var mauiDensePanel = new VerticalStackLayout { Spacing = 8 };
        var proDensePanel = new VerticalStackLayout { Spacing = 8 };
        content.Children.Add(CreateTwoColumnSection(
            "Dense MAUI Labels",
            mauiDensePanel,
            "Dense ProTextBlocks",
            proDensePanel));

        for (var i = 0; i < 36; i++)
        {
            var text = $"Row {i + 1:00}: {DenseText(i)}";
            var mauiLabel = CreateDisplayLabel(text);
            var proText = ProTextMauiScaffold.CreateTextControl("ProTextBlock");

            _mauiDenseLabels.Add(mauiLabel);
            _proDenseTextBlocks.Add(proText);
            mauiDensePanel.Children.Add(mauiLabel);
            proDensePanel.Children.Add(proText);
        }

        return content;
    }

    private static Border CreateTwoColumnSection(string leftTitle, View leftContent, string rightTitle, View rightContent)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 18
        };

        var leftPanel = CreatePanel(leftTitle, leftContent);
        var rightPanel = CreatePanel(rightTitle, rightContent);
        Grid.SetColumn(rightPanel, 1);
        grid.Children.Add(leftPanel);
        grid.Children.Add(rightPanel);

        return new Border
        {
            Content = grid
        };
    }

    private static Border CreatePanel(string title, View content)
    {
        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = new SolidColorBrush(Colors.Gainsboro),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
            Padding = new Thickness(18),
            Content = new VerticalStackLayout
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

    private static VerticalStackLayout CreateEditorGroup(string title, params View[] controls)
    {
        var panel = new VerticalStackLayout { Spacing = 8 };
        panel.Children.Add(CreateSectionTitle(title));

        foreach (var control in controls)
        {
            panel.Children.Add(control);
        }

        return panel;
    }

    private static HorizontalStackLayout CreateCheckRow(CheckBox checkBox, string text)
    {
        return new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                checkBox,
                CreateCaption(text)
            }
        };
    }

    private void RebuildDenseRows()
    {
        for (var i = 0; i < _mauiDenseLabels.Count; i++)
        {
            var text = $"Row {i + 1:00}: {DenseText(i)}";
            _mauiDenseLabels[i].Text = text;
            ProTextMauiScaffold.ApplyText(_proDenseTextBlocks[i], text);
        }
    }

    private void UpdateContent()
    {
        var corpus = _corpusPicker.SelectedItem as CorpusItem ?? _corpora[1];
        var fontSize = Math.Round(_fontSizeSlider.Value);
        var maxLines = (int)Math.Round(_maxLinesSlider.Value);
        var lineBreakMode = _wrapCheckBox.IsChecked ? LineBreakMode.WordWrap : LineBreakMode.NoWrap;
        var lineHeight = 1.42d;
        var useGlobalCache = _cacheCheckBox.IsChecked;

        _fontSizeValueLabel.Text = $"{fontSize:0} px";
        _maxLinesValueLabel.Text = maxLines == 0 ? "Unlimited" : maxLines.ToString();

        ApplyLabelSettings(_mauiLabel, corpus.Text, fontSize, lineHeight, lineBreakMode, maxLines);
        ApplyLabelStyle(_mauiInlineLabel, fontSize, lineHeight, lineBreakMode, maxLines);
        ApplyEditorSettings(_mauiEditor, corpus.Text, fontSize);
        ProTextMauiScaffold.ApplyTextSettings(_proText, corpus.Text, fontSize, lineHeight, lineBreakMode, maxLines, useGlobalCache);
        ProTextMauiScaffold.ApplyStyleSettings(_proInlineText, fontSize, lineHeight, lineBreakMode, maxLines, useGlobalCache);
        ProTextMauiScaffold.ApplyTextSettings(_proPresenter, "Presenter selection, caret, preedit, password masking, and rich inline presentation share the ProText layout core.", fontSize, lineHeight, lineBreakMode, maxLines, useGlobalCache);
        ProTextMauiScaffold.ApplyTextSettings(_proTextBox, corpus.Text, fontSize, lineHeight, lineBreakMode, maxLines, useGlobalCache);
        ProTextMauiScaffold.ApplyTextSettings(_proEditorTextBox, corpus.Text, fontSize, lineHeight, lineBreakMode, maxLines, useGlobalCache);

        for (var i = 0; i < _mauiDenseLabels.Count; i++)
        {
            var text = $"Row {i + 1:00}: {DenseText(i)}";
            var denseFontSize = Math.Max(11, fontSize - 2);
            ApplyLabelSettings(_mauiDenseLabels[i], text, denseFontSize, 1.35, lineBreakMode, 2);
            ProTextMauiScaffold.ApplyTextSettings(_proDenseTextBlocks[i], text, denseFontSize, 1.35, lineBreakMode, 2, useGlobalCache);
        }

        UpdateCacheStatus();
    }

    private void UpdateCacheStatus()
    {
        _cacheStatusLabel.Text = ProTextMauiScaffold.GetCacheStatus();
    }

    private static void ApplyLabelSettings(Label label, string text, double fontSize, double lineHeight, LineBreakMode lineBreakMode, int maxLines)
    {
        label.Text = text;
        ApplyLabelStyle(label, fontSize, lineHeight, lineBreakMode, maxLines);
    }

    private static void ApplyLabelStyle(Label label, double fontSize, double lineHeight, LineBreakMode lineBreakMode, int maxLines)
    {
        label.FontSize = fontSize;
        label.LineHeight = lineHeight;
        label.LineBreakMode = lineBreakMode;
        label.MaxLines = maxLines == 0 ? -1 : maxLines;
    }

    private static void ApplyEditorSettings(Editor editor, string text, double fontSize)
    {
        editor.Text = text;
        editor.FontSize = fontSize;
        editor.TextColor = Colors.Black;
        editor.AutoSize = EditorAutoSizeOption.TextChanges;
    }

    private static Label CreateDisplayLabel(string text = "")
    {
        return new Label
        {
            Text = text,
            LineBreakMode = LineBreakMode.WordWrap,
            TextColor = Colors.Black
        };
    }

    private static Label CreateInlineLabel()
    {
        var label = CreateDisplayLabel();
        label.FormattedText = new FormattedString
        {
            Spans =
            {
                new Span { Text = "Inline content: " },
                new Span { Text = "bold", FontAttributes = FontAttributes.Bold },
                new Span { Text = ", " },
                new Span { Text = "italic", FontAttributes = FontAttributes.Italic },
                new Span { Text = ", and " },
                new Span { Text = "underlined", TextDecorations = TextDecorations.Underline },
                new Span { Text = " spans use matching formatted styling in both comparison columns." }
            }
        };
        return label;
    }

    private static Editor CreateEditor()
    {
        return new Editor
        {
            AutoSize = EditorAutoSizeOption.TextChanges,
            TextColor = Colors.Black,
            BackgroundColor = Colors.White
        };
    }

    private static Label CreateTitle(string text)
    {
        return new Label
        {
            Text = text,
            FontSize = 24,
            TextColor = Colors.Black
        };
    }

    private static Label CreateSectionTitle(string text)
    {
        return new Label
        {
            Text = text,
            FontSize = 13,
            TextColor = Colors.DarkSlateGray
        };
    }

    private static Label CreateCaption(string text = "")
    {
        return new Label
        {
            Text = text,
            FontSize = 12,
            TextColor = Colors.DimGray,
            VerticalOptions = LayoutOptions.Center
        };
    }

    private static string DenseText(int index)
    {
        return (index % 4) switch
        {
            0 => "cache hit, wrapped label, two measured lines",
            1 => "localized preview: Hello, Привет, مرحبا, こんにちは",
            2 => "formatted span intent with bold, italic, underline markers",
            _ => "scrolling surface row for repeated text measurement"
        };
    }

    private sealed record CorpusItem(string Name, string Text);
}

internal static class ProTextMauiScaffold
{
    private const string AssemblyName = "ProText.MAUI";

    public static void ConfigureBuilder(MauiAppBuilder builder)
    {
        ResolveType("ProTextMauiAppBuilderExtensions")
            ?.GetMethod("UseProTextMaui", BindingFlags.Static | BindingFlags.Public)
            ?.Invoke(null, [builder]);
    }

    public static View CreateTextControl(string controlName)
    {
        if (TryCreateControl(controlName) is { } control)
        {
            return control;
        }

        return CreatePendingLabel(controlName);
    }

    public static View CreateInlineTextControl(string controlName)
    {
        if (TryCreateControl(controlName) is { } control)
        {
            AddInlineSpans(control);
            return control;
        }

        var fallback = CreatePendingLabel(controlName);
        fallback.Text = "Inline content: bold, italic, and underlined spans will bind to ProText.MAUI when src/ProText.MAUI is present.";
        return fallback;
    }

    public static void ApplyText(View element, string text)
    {
        if (element is Label label)
        {
            label.Text = text;
            return;
        }

        if (element is Editor editor)
        {
            editor.Text = text;
            return;
        }

        SetProperty(element, "Text", text);
    }

    public static void ApplyTextSettings(View element, string text, double fontSize, double lineHeight, LineBreakMode lineBreakMode, int maxLines, bool useGlobalCache)
    {
        if (element is Label label)
        {
            label.Text = text;
            ApplyLabelStyle(label, fontSize, lineHeight, lineBreakMode, maxLines);
            return;
        }

        if (element is Editor editor)
        {
            editor.Text = text;
            editor.FontSize = fontSize;
            editor.TextColor = Colors.Black;
            return;
        }

        SetProperty(element, "Text", text);
        ApplyStyleSettings(element, fontSize, lineHeight, lineBreakMode, maxLines, useGlobalCache);
    }

    public static void ApplyStyleSettings(View element, double fontSize, double lineHeight, LineBreakMode lineBreakMode, int maxLines, bool useGlobalCache)
    {
        if (element is Label label)
        {
            ApplyLabelStyle(label, fontSize, lineHeight, lineBreakMode, maxLines);
            return;
        }

        SetProperty(element, "FontSize", fontSize);
        SetProperty(element, "LineHeight", Math.Round(fontSize * lineHeight));
        SetProperty(element, "PretextLineHeightMultiplier", lineHeight);
        SetProperty(element, "LineBreakMode", lineBreakMode);
        SetProperty(element, "MaxLines", maxLines);
        SetProperty(element, "UseGlobalCache", useGlobalCache);
        SetProperty(element, "TextColor", Colors.Black);
        SetProperty(element, "Foreground", Colors.Black);
        SetProperty(element, "Foreground", new SolidColorBrush(Colors.Black));
        SetProperty(element, "AutoSize", EditorAutoSizeOption.TextChanges);
        SetProperty(element, "AcceptsReturn", true);
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
            return "ProText.MAUI unavailable";
        }

        var count = ReadProperty(snapshot, "Count");
        var hits = ReadProperty(snapshot, "Hits");
        var misses = ReadProperty(snapshot, "Misses");
        return $"Cache entries {count}, hits {hits}, misses {misses}";
    }

    private static void ApplyLabelStyle(Label label, double fontSize, double lineHeight, LineBreakMode lineBreakMode, int maxLines)
    {
        label.FontSize = fontSize;
        label.LineHeight = lineHeight;
        label.LineBreakMode = lineBreakMode;
        label.MaxLines = maxLines == 0 ? -1 : maxLines;
    }

    private static View? TryCreateControl(string controlName)
    {
        var type = ResolveType(controlName);
        return type is null ? null : Activator.CreateInstance(type) as View;
    }

    private static Type? ResolveType(string controlName)
    {
        var type = Type.GetType($"{AssemblyName}.{controlName}, {AssemblyName}", throwOnError: false);
        if (type is not null)
        {
            return type;
        }

        try
        {
            return Assembly.Load(AssemblyName).GetType($"{AssemblyName}.{controlName}", throwOnError: false);
        }
        catch
        {
            return null;
        }
    }

    private static Label CreatePendingLabel(string controlName)
    {
        return new Label
        {
            Text = $"{controlName} unavailable: build with the ProText.MAUI project reference to activate this column.",
            LineBreakMode = LineBreakMode.WordWrap,
            TextColor = Colors.DimGray
        };
    }

    private static void AddInlineSpans(View control)
    {
        var formattedText = new FormattedString
        {
            Spans =
            {
                new Span { Text = "Inline content: " },
                new Span { Text = "bold", FontAttributes = FontAttributes.Bold },
                new Span { Text = ", " },
                new Span { Text = "italic", FontAttributes = FontAttributes.Italic },
                new Span { Text = ", and " },
                new Span { Text = "underlined", TextDecorations = TextDecorations.Underline },
                new Span { Text = " spans use matching formatted styling in both comparison columns." }
            }
        };

        if (SetProperty(control, "FormattedText", formattedText))
        {
            return;
        }

        SetProperty(control, "Text", "Inline content: bold, italic, and underlined spans use the ProText rich inline path.");
    }

    private static bool SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is not { CanWrite: true })
        {
            return false;
        }

        if (value is null)
        {
            if (!property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) is not null)
            {
                property.SetValue(target, null);
                return true;
            }

            return false;
        }

        if (property.PropertyType.IsInstanceOfType(value))
        {
            property.SetValue(target, value);
            return true;
        }

        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (targetType.IsEnum && value is string enumName && Enum.TryParse(targetType, enumName, ignoreCase: true, out var enumValue))
        {
            property.SetValue(target, enumValue);
            return true;
        }

        if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
        {
            property.SetValue(target, Convert.ChangeType(value, targetType));
            return true;
        }

        return false;
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
