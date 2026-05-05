using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
[ShortRunJob]
public class UnoTextBlockLayoutBenchmarks
{
    private readonly string _text = string.Join(' ', Enumerable.Repeat(
        "Uno text layout pressure comes from repeated measurement, width probes, and redraws across dense interfaces.",
        18));

    private TextBlock _winUiTextBlock = null!;
    private TextBlock _winUiRichTextBlock = null!;

#if PROTEXT_UNO_AVAILABLE
    private FrameworkElement _proTextBlock = null!;
    private FrameworkElement _proTextBlockLocalCache = null!;
    private FrameworkElement _proRichTextBlock = null!;
#endif

    [Params(160, 320, 640)]
    public double Width { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _winUiTextBlock = UnoBenchmarkControls.CreateWinUITextBlock(_text);
        _winUiRichTextBlock = UnoBenchmarkControls.CreateWinUIRichTextBlock();

#if PROTEXT_UNO_AVAILABLE
        UnoBenchmarkControls.ClearProTextCache();
        _proTextBlock = UnoBenchmarkControls.CreateProTextControl("ProTextBlock", _text);
        _proTextBlockLocalCache = UnoBenchmarkControls.CreateProTextControl("ProTextBlock", _text, useGlobalCache: false);
        _proRichTextBlock = UnoBenchmarkControls.CreateProTextRichBlock();
        _ = UnoBenchmarkControls.Measure(_proTextBlock, Width);
#endif
    }

    [Benchmark(Baseline = true)]
    public Size WinUITextBlockMeasure()
    {
        return UnoBenchmarkControls.Measure(_winUiTextBlock, Width);
    }

    [Benchmark]
    public Size WinUIRichTextBlockMeasure()
    {
        return UnoBenchmarkControls.Measure(_winUiRichTextBlock, Width);
    }

#if PROTEXT_UNO_AVAILABLE
    [Benchmark]
    public Size ProTextBlockGlobalCacheMeasure()
    {
        return UnoBenchmarkControls.Measure(_proTextBlock, Width);
    }

    [Benchmark]
    public Size ProTextBlockLocalCacheMeasure()
    {
        return UnoBenchmarkControls.Measure(_proTextBlockLocalCache, Width);
    }

    [Benchmark]
    public Size ProTextBlockRichMeasure()
    {
        return UnoBenchmarkControls.Measure(_proRichTextBlock, Width);
    }
#endif
}

#if PROTEXT_UNO_AVAILABLE
[MemoryDiagnoser]
[ShortRunJob]
public class UnoPresenterBenchmarks
{
    private FrameworkElement _presenter = null!;
    private readonly string _text = string.Join(' ', Enumerable.Repeat(
        "Presenter text keeps selection, caret, preedit, password masking, and hit testing on the shared ProText path.",
        16));

    [Params(240, 480, 960)]
    public double Width { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _presenter = UnoBenchmarkControls.CreateProTextControl("ProTextPresenter", _text);
        UnoBenchmarkControls.SetProperty(_presenter, "SelectionStart", 30);
        UnoBenchmarkControls.SetProperty(_presenter, "SelectionEnd", 180);
        UnoBenchmarkControls.SetProperty(_presenter, "CaretIndex", 220);
        UnoBenchmarkControls.SetProperty(_presenter, "PreeditText", "IME");
        UnoBenchmarkControls.SetProperty(_presenter, "PreeditTextCursorPosition", 2);
        _ = UnoBenchmarkControls.Measure(_presenter, Width);
    }

    [Benchmark(Baseline = true)]
    public Size ProTextPresenterMeasure()
    {
        return UnoBenchmarkControls.Measure(_presenter, Width);
    }

    [Benchmark]
    public object? ProTextPresenterCaretBounds()
    {
        _ = UnoBenchmarkControls.Measure(_presenter, Width);
        return UnoBenchmarkControls.Invoke(_presenter, "GetCaretBounds", 128);
    }

    [Benchmark]
    public object? ProTextPresenterHitTest()
    {
        _ = UnoBenchmarkControls.Measure(_presenter, Width);
        return UnoBenchmarkControls.Invoke(_presenter, "GetCharacterIndex", new Point(Width / 2, 36));
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class UnoTextBoxBenchmarks
{
    private readonly string _text = string.Join(' ', Enumerable.Repeat(
        "Editable dense surfaces measure text boxes repeatedly while caret, selection, wrapping, and placeholder state stay synchronized.",
        18));

    private TextBox _winUiTextBox = null!;
    private FrameworkElement _proTextBox = null!;

    [Params(220, 440, 880)]
    public double Width { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _winUiTextBox = UnoBenchmarkControls.CreateWinUITextBox(_text);
        _proTextBox = UnoBenchmarkControls.CreateProTextControl("ProTextBox", _text);
        UnoBenchmarkControls.SetProperty(_proTextBox, "AcceptsReturn", true);
        UnoBenchmarkControls.SetProperty(_proTextBox, "SelectionStart", 32);
        UnoBenchmarkControls.SetProperty(_proTextBox, "SelectionEnd", 220);
        UnoBenchmarkControls.SetProperty(_proTextBox, "CaretIndex", 220);
    }

    [Benchmark(Baseline = true)]
    public Size WinUITextBoxMeasure()
    {
        return UnoBenchmarkControls.Measure(_winUiTextBox, Width);
    }

    [Benchmark]
    public Size ProTextBoxMeasure()
    {
        return UnoBenchmarkControls.Measure(_proTextBox, Width);
    }
}
#endif

internal static class UnoBenchmarkControls
{
    private const string ProTextAssemblyName = "ProText.Uno";

    public static TextBlock CreateWinUITextBlock(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 16,
            LineHeight = 22,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Colors.Black)
        };
    }

    public static TextBlock CreateWinUIRichTextBlock()
    {
        var textBlock = CreateWinUITextBlock(string.Empty);
        textBlock.MaxLines = 8;
        textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
        textBlock.Inlines.Add(new Run { Text = "Rich inline content mixes " });
        textBlock.Inlines.Add(new Bold { Inlines = { new Run { Text = "bold" } } });
        textBlock.Inlines.Add(new Run { Text = ", " });
        textBlock.Inlines.Add(new Italic { Inlines = { new Run { Text = "italic" } } });
        textBlock.Inlines.Add(new Run { Text = ", " });
        textBlock.Inlines.Add(new Underline { Inlines = { new Run { Text = "underlined" } } });
        textBlock.Inlines.Add(new Run { Text = ", trimming, and rich text measurement." });
        return textBlock;
    }

    public static TextBox CreateWinUITextBox(string text)
    {
        return new TextBox
        {
            Text = text,
            AcceptsReturn = true,
            FontSize = 16,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Colors.Black)
        };
    }

#if PROTEXT_UNO_AVAILABLE
    public static FrameworkElement CreateProTextControl(string controlName, string text, bool useGlobalCache = true)
    {
        var type = ResolveProTextType(controlName)
            ?? throw new InvalidOperationException($"ProText.Uno type '{controlName}' was not found.");

        var control = Activator.CreateInstance(type) as FrameworkElement
            ?? throw new InvalidOperationException($"ProText.Uno type '{controlName}' is not a FrameworkElement.");

        SetProperty(control, "Text", text);
        SetProperty(control, "FontSize", 16d);
        SetProperty(control, "LineHeight", 22d);
        SetProperty(control, "TextWrapping", TextWrapping.Wrap);
        SetProperty(control, "Foreground", new SolidColorBrush(Colors.Black));
        SetProperty(control, "UseGlobalCache", useGlobalCache);
        return control;
    }

    public static FrameworkElement CreateProTextRichBlock()
    {
        var control = CreateProTextControl("ProTextBlock", string.Empty);
        SetProperty(control, "MaxLines", 8);
        SetProperty(control, "TextTrimming", TextTrimming.CharacterEllipsis);

        var inlines = control.GetType().GetProperty("Inlines", BindingFlags.Instance | BindingFlags.Public)?.GetValue(control);
        var addMethod = inlines?.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
        if (addMethod is null)
        {
            SetProperty(control, "Text", "Rich inline content mixes bold, italic, underlined, trimming, and rich text measurement.");
            return control;
        }

        addMethod.Invoke(inlines, [new Run { Text = "Rich inline content mixes " }]);
        addMethod.Invoke(inlines, [new Bold { Inlines = { new Run { Text = "bold" } } }]);
        addMethod.Invoke(inlines, [new Run { Text = ", " }]);
        addMethod.Invoke(inlines, [new Italic { Inlines = { new Run { Text = "italic" } } }]);
        addMethod.Invoke(inlines, [new Run { Text = ", " }]);
        addMethod.Invoke(inlines, [new Underline { Inlines = { new Run { Text = "underlined" } } }]);
        addMethod.Invoke(inlines, [new Run { Text = ", trimming, and rich text measurement." }]);
        return control;
    }

    public static void ClearProTextCache()
    {
        ResolveProTextType("ProTextCache")?.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
    }

    public static void SetProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is { CanWrite: true } && property.PropertyType.IsInstanceOfType(value))
        {
            property.SetValue(target, value);
        }
    }

    public static object? Invoke(object target, string methodName, params object[] arguments)
    {
        return target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)?.Invoke(target, arguments);
    }

    private static Type? ResolveProTextType(string typeName)
    {
        return Type.GetType($"{ProTextAssemblyName}.{typeName}, {ProTextAssemblyName}", throwOnError: false);
    }
#endif

    public static Size Measure(FrameworkElement element, double width)
    {
        element.InvalidateMeasure();
        element.Measure(new Size(width, double.PositiveInfinity));
        return element.DesiredSize;
    }
}
