using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
[ShortRunJob]
public class MauiLabelLayoutBenchmarks
{
    private readonly string _text = string.Join(' ', Enumerable.Repeat(
        "MAUI text layout pressure comes from repeated measurement, width probes, and redraws across dense cross-platform interfaces.",
        18));

    private Label _mauiLabel = null!;
    private Label _mauiFormattedLabel = null!;

#if PROTEXT_MAUI_AVAILABLE
    private View _proTextBlock = null!;
    private View _proTextBlockLocalCache = null!;
    private View _proFormattedTextBlock = null!;
#endif

    [Params(160, 320, 640)]
    public double Width { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mauiLabel = MauiBenchmarkControls.CreateMauiLabel(_text);
        _mauiFormattedLabel = MauiBenchmarkControls.CreateMauiFormattedLabel();

#if PROTEXT_MAUI_AVAILABLE
        MauiBenchmarkControls.ClearProTextCache();
        _proTextBlock = MauiBenchmarkControls.CreateProTextControl("ProTextBlock", _text);
        _proTextBlockLocalCache = MauiBenchmarkControls.CreateProTextControl("ProTextBlock", _text, useGlobalCache: false);
        _proFormattedTextBlock = MauiBenchmarkControls.CreateProTextFormattedBlock();
        _ = MauiBenchmarkControls.Measure(_proTextBlock, Width);
#endif
    }

    [Benchmark(Baseline = true)]
    public Size MauiLabelMeasure()
    {
        return MauiBenchmarkControls.Measure(_mauiLabel, Width);
    }

    [Benchmark]
    public Size MauiFormattedLabelMeasure()
    {
        return MauiBenchmarkControls.Measure(_mauiFormattedLabel, Width);
    }

#if PROTEXT_MAUI_AVAILABLE
    [Benchmark]
    public Size ProTextBlockGlobalCacheMeasure()
    {
        return MauiBenchmarkControls.Measure(_proTextBlock, Width);
    }

    [Benchmark]
    public Size ProTextBlockLocalCacheMeasure()
    {
        return MauiBenchmarkControls.Measure(_proTextBlockLocalCache, Width);
    }

    [Benchmark]
    public Size ProTextBlockFormattedMeasure()
    {
        return MauiBenchmarkControls.Measure(_proFormattedTextBlock, Width);
    }
#endif
}

#if PROTEXT_MAUI_AVAILABLE
[MemoryDiagnoser]
[ShortRunJob]
public class MauiPresenterBenchmarks
{
    private View _presenter = null!;
    private readonly string _text = string.Join(' ', Enumerable.Repeat(
        "Presenter text keeps selection, caret, preedit, password masking, and hit testing on the shared ProText path.",
        16));

    [Params(240, 480, 960)]
    public double Width { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _presenter = MauiBenchmarkControls.CreateProTextControl("ProTextPresenter", _text);
        MauiBenchmarkControls.SetProperty(_presenter, "SelectionStart", 30);
        MauiBenchmarkControls.SetProperty(_presenter, "SelectionEnd", 180);
        MauiBenchmarkControls.SetProperty(_presenter, "CaretIndex", 220);
        MauiBenchmarkControls.SetProperty(_presenter, "PreeditText", "IME");
        MauiBenchmarkControls.SetProperty(_presenter, "PreeditTextCursorPosition", 2);
        _ = MauiBenchmarkControls.Measure(_presenter, Width);
    }

    [Benchmark(Baseline = true)]
    public Size ProTextPresenterMeasure()
    {
        return MauiBenchmarkControls.Measure(_presenter, Width);
    }

    [Benchmark]
    public object? ProTextPresenterCaretBounds()
    {
        _ = MauiBenchmarkControls.Measure(_presenter, Width);
        return MauiBenchmarkControls.Invoke(_presenter, "GetCaretBounds", 128);
    }

    [Benchmark]
    public object? ProTextPresenterHitTest()
    {
        _ = MauiBenchmarkControls.Measure(_presenter, Width);
        return MauiBenchmarkControls.Invoke(_presenter, "GetCharacterIndex", new Point(Width / 2, 36));
    }
}
#endif

[MemoryDiagnoser]
[ShortRunJob]
public class MauiEditorBenchmarks
{
    private readonly string _text = string.Join(' ', Enumerable.Repeat(
        "Editable dense surfaces measure text boxes repeatedly while caret, selection, wrapping, and placeholder state stay synchronized.",
        18));

    private Editor _mauiEditor = null!;

#if PROTEXT_MAUI_AVAILABLE
    private View _proTextBox = null!;
#endif

    [Params(220, 440, 880)]
    public double Width { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mauiEditor = MauiBenchmarkControls.CreateMauiEditor(_text);

#if PROTEXT_MAUI_AVAILABLE
        _proTextBox = MauiBenchmarkControls.CreateProTextControl("ProTextBox", _text);
        MauiBenchmarkControls.SetProperty(_proTextBox, "SelectionStart", 32);
        MauiBenchmarkControls.SetProperty(_proTextBox, "SelectionEnd", 220);
        MauiBenchmarkControls.SetProperty(_proTextBox, "CaretIndex", 220);
#endif
    }

    [Benchmark(Baseline = true)]
    public Size MauiEditorMeasure()
    {
        return MauiBenchmarkControls.Measure(_mauiEditor, Width);
    }

#if PROTEXT_MAUI_AVAILABLE
    [Benchmark]
    public Size ProTextBoxMeasure()
    {
        return MauiBenchmarkControls.Measure(_proTextBox, Width);
    }
#endif
}

internal static class MauiBenchmarkControls
{
    private const string ProTextAssemblyName = "ProText.MAUI";

    public static Label CreateMauiLabel(string text)
    {
        return new Label
        {
            Text = text,
            FontSize = 16,
            LineHeight = 1.42,
            LineBreakMode = LineBreakMode.WordWrap,
            TextColor = Colors.Black
        };
    }

    public static Label CreateMauiFormattedLabel()
    {
        var label = CreateMauiLabel(string.Empty);
        label.MaxLines = 8;
        label.LineBreakMode = LineBreakMode.TailTruncation;
        label.FormattedText = CreateFormattedText();
        return label;
    }

    public static Editor CreateMauiEditor(string text)
    {
        return new Editor
        {
            Text = text,
            AutoSize = EditorAutoSizeOption.TextChanges,
            FontSize = 16,
            TextColor = Colors.Black
        };
    }

#if PROTEXT_MAUI_AVAILABLE
    public static View CreateProTextControl(string controlName, string text, bool useGlobalCache = true)
    {
        var type = ResolveProTextType(controlName)
            ?? throw new InvalidOperationException($"ProText.MAUI type '{controlName}' was not found.");

        var control = Activator.CreateInstance(type) as View
            ?? throw new InvalidOperationException($"ProText.MAUI type '{controlName}' is not a View.");

        SetProperty(control, "Text", text);
        SetProperty(control, "FontSize", 16d);
        SetProperty(control, "LineHeight", 1.42d);
        SetProperty(control, "PretextLineHeightMultiplier", 1.42d);
        SetProperty(control, "LineBreakMode", LineBreakMode.WordWrap);
        SetProperty(control, "TextColor", Colors.Black);
        SetProperty(control, "Foreground", Colors.Black);
        SetProperty(control, "Foreground", new SolidColorBrush(Colors.Black));
        SetProperty(control, "UseGlobalCache", useGlobalCache);
        SetProperty(control, "AutoSize", EditorAutoSizeOption.TextChanges);
        SetProperty(control, "AcceptsReturn", true);
        return control;
    }

    public static View CreateProTextFormattedBlock()
    {
        var control = CreateProTextControl("ProTextBlock", string.Empty);
        SetProperty(control, "MaxLines", 8);
        SetProperty(control, "LineBreakMode", LineBreakMode.TailTruncation);

        if (!SetProperty(control, "FormattedText", CreateFormattedText()))
        {
            SetProperty(control, "Text", "Formatted content mixes bold, italic, underlined, trimming, and rich text measurement.");
        }

        return control;
    }

    public static void ClearProTextCache()
    {
        ResolveProTextType("ProTextCache")?.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
    }

    public static bool SetProperty(object target, string propertyName, object? value)
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
        if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
        {
            property.SetValue(target, Convert.ChangeType(value, targetType));
            return true;
        }

        return false;
    }

    public static object? Invoke(object target, string methodName, params object[] arguments)
    {
        return target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)?.Invoke(target, arguments);
    }

    private static Type? ResolveProTextType(string typeName)
    {
        var type = Type.GetType($"{ProTextAssemblyName}.{typeName}, {ProTextAssemblyName}", throwOnError: false);
        if (type is not null)
        {
            return type;
        }

        try
        {
            return Assembly.Load(ProTextAssemblyName).GetType($"{ProTextAssemblyName}.{typeName}", throwOnError: false);
        }
        catch
        {
            return null;
        }
    }
#endif

    public static Size Measure(View element, double width)
    {
        element.GetType().GetMethod(
            "InvalidateMeasure",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null)?.Invoke(element, null);
        return element.Measure(width, double.PositiveInfinity);
    }

    private static FormattedString CreateFormattedText()
    {
        return new FormattedString
        {
            Spans =
            {
                new Span { Text = "Formatted content mixes " },
                new Span { Text = "bold", FontAttributes = FontAttributes.Bold },
                new Span { Text = ", " },
                new Span { Text = "italic", FontAttributes = FontAttributes.Italic },
                new Span { Text = ", " },
                new Span { Text = "underlined", TextDecorations = TextDecorations.Underline },
                new Span { Text = ", trimming, and rich text measurement." }
            }
        };
    }
}
