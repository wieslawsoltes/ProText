using System.Reflection;

namespace ProText.MAUI.Tests;

public class ProTextMauiApiSurfaceTests
{
    [Fact]
    public void ExposesExpectedControls()
    {
        Assert.Equal("ProText.MAUI.ProTextBlock", ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextBlock").FullName);
        Assert.Equal("ProText.MAUI.ProTextPresenter", ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextPresenter").FullName);
        Assert.Equal("ProText.MAUI.ProTextBox", ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextBox").FullName);
    }

    [Fact]
    public void ProTextBlockExposesSharedCacheAndMauiTextProperties()
    {
        var properties = PublicProperties("ProText.MAUI.ProTextBlock");

        Assert.Contains("Text", properties);
        Assert.Contains("FormattedText", properties);
        Assert.Contains("UseGlobalCache", properties);
        Assert.Contains("UsePretextRendering", properties);
        Assert.Contains("PretextWhiteSpace", properties);
        Assert.Contains("PretextWordBreak", properties);
        Assert.Contains("PretextLineHeightMultiplier", properties);
        Assert.Contains("LineBreakMode", properties);
        Assert.Contains("CharacterSpacing", properties);
        Assert.Contains("LetterSpacing", properties);
        Assert.Contains("Foreground", properties);
    }

    [Fact]
    public void ProTextPresenterExposesPresenterApis()
    {
        var methods = PublicMethods("ProText.MAUI.ProTextPresenter");

        Assert.Contains("ShowCaret", methods);
        Assert.Contains("HideCaret", methods);
        Assert.Contains("MoveCaretToTextPosition", methods);
        Assert.Contains("MoveCaretToPoint", methods);
        Assert.Contains("MoveCaretHorizontal", methods);
        Assert.Contains("MoveCaretVertical", methods);
        Assert.Contains("GetCharacterIndex", methods);
        Assert.Contains("GetCaretBounds", methods);

        var properties = PublicProperties("ProText.MAUI.ProTextPresenter");
        Assert.Contains("CaretBlinkInterval", properties);
        Assert.Contains("SelectionForegroundBrush", properties);
    }

    [Fact]
    public void ProTextPresenterCaretCanToggleBeforeHandlerIsAttached()
    {
        var presenterType = ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextPresenter");
        var presenter = Activator.CreateInstance(presenterType)!;

        presenterType.GetMethod("ShowCaret", BindingFlags.Instance | BindingFlags.Public)!.Invoke(presenter, null);
        presenterType.GetMethod("HideCaret", BindingFlags.Instance | BindingFlags.Public)!.Invoke(presenter, null);
    }

    [Fact]
    public void ProTextBoxExposesTextBoxLikeHelpers()
    {
        var methods = PublicMethods("ProText.MAUI.ProTextBox");

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
        var snapshotType = ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextCacheSnapshot");
        var snapshot = Activator.CreateInstance(snapshotType, 1, 2, 3L, 4L)!;

        Assert.Equal(1, GetProperty<int>(snapshot, "Count"));
        Assert.Equal(2, GetProperty<int>(snapshot, "MaxEntryCount"));
        Assert.Equal(3, GetProperty<long>(snapshot, "Hits"));
        Assert.Equal(4, GetProperty<long>(snapshot, "Misses"));
    }

    [Fact]
    public void ProTextCacheExposesDiagnosticSnapshot()
    {
        var cacheType = ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextCache");
        var snapshotType = ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextCacheSnapshot");

        Assert.NotNull(cacheType.GetProperty("MaxEntryCount", BindingFlags.Static | BindingFlags.Public));
        Assert.NotNull(cacheType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public));

        var getSnapshot = cacheType.GetMethod("GetSnapshot", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(getSnapshot);
        Assert.Equal(snapshotType, getSnapshot.ReturnType);
    }

    [Fact]
    public void BrushBindablePropertiesDoNotShareMutableDefaultBrushInstances()
    {
        var bindablePropertyTypeName = "Microsoft.Maui.Controls.BindableProperty";
        var controls = new[]
        {
            ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextBlock"),
            ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextPresenter"),
            ProTextMauiTestAssembly.RequiredType("ProText.MAUI.ProTextBox")
        };

        foreach (var control in controls)
        {
            var bindablePropertyFields = control
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                .Where(field =>
                    field.FieldType.FullName == bindablePropertyTypeName &&
                    field.DeclaringType?.Namespace == "ProText.MAUI");

            foreach (var field in bindablePropertyFields)
            {
                var bindableProperty = field.GetValue(null);
                var defaultValue = bindableProperty?.GetType().GetProperty("DefaultValue")?.GetValue(bindableProperty);

                Assert.False(
                    defaultValue?.GetType().FullName is string defaultValueType &&
                    defaultValueType.StartsWith("Microsoft.Maui.Controls.", StringComparison.Ordinal) &&
                    defaultValueType.EndsWith("Brush", StringComparison.Ordinal),
                    $"{control.FullName}.{field.Name} uses a shared mutable MAUI brush default.");
            }
        }
    }

    private static HashSet<string> PublicProperties(string typeName)
    {
        return ProTextMauiTestAssembly.RequiredType(typeName)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(static property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> PublicMethods(string typeName)
    {
        return ProTextMauiTestAssembly.RequiredType(typeName)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(static method => method.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static T GetProperty<T>(object instance, string propertyName)
    {
        return (T)instance.GetType().GetProperty(propertyName)!.GetValue(instance)!;
    }
}
