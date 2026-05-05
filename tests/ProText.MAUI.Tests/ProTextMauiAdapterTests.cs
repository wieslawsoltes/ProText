using System.Reflection;
using ProText.Core;

namespace ProText.MAUI.Tests;

public class ProTextMauiAdapterTests
{
    [Fact]
    public void MapsLineBreakModeToCoreWrapping()
    {
        var method = RequiredMethod(typeof(ProTextWrapping), "Microsoft.Maui.LineBreakMode",
            "ToWrapping", "ToCoreWrapping", "ToCoreTextWrapping", "ToCore");

        Assert.Equal(ProTextWrapping.NoWrap, InvokeEnum<ProTextWrapping>(method, "NoWrap"));
        Assert.Equal(ProTextWrapping.Wrap, InvokeEnum<ProTextWrapping>(method, "WordWrap"));
        Assert.Equal(ProTextWrapping.Wrap, InvokeEnum<ProTextWrapping>(method, "CharacterWrap"));
        Assert.Equal(ProTextWrapping.NoWrap, InvokeEnum<ProTextWrapping>(method, "TailTruncation"));
    }

    [Fact]
    public void MapsLineBreakModeToCoreTrimming()
    {
        var method = RequiredMethod(typeof(ProTextTrimming), "Microsoft.Maui.LineBreakMode",
            "ToTrimming", "ToCoreTrimming", "ToCoreTextTrimming");

        Assert.Equal(ProTextTrimming.None, InvokeEnum<ProTextTrimming>(method, "NoWrap"));
        Assert.Equal(ProTextTrimming.None, InvokeEnum<ProTextTrimming>(method, "WordWrap"));
        Assert.Equal(ProTextTrimming.None, InvokeEnum<ProTextTrimming>(method, "CharacterWrap"));
        Assert.Equal(ProTextTrimming.CharacterEllipsis, InvokeEnum<ProTextTrimming>(method, "HeadTruncation"));
        Assert.Equal(ProTextTrimming.CharacterEllipsis, InvokeEnum<ProTextTrimming>(method, "TailTruncation"));
        Assert.Equal(ProTextTrimming.CharacterEllipsis, InvokeEnum<ProTextTrimming>(method, "MiddleTruncation"));
    }

    [Fact]
    public void MapsTextAlignmentToCore()
    {
        var method = RequiredMethod(typeof(ProTextTextAlignment), "Microsoft.Maui.TextAlignment",
            "ToCore", "ToCoreTextAlignment", "ToTextAlignment");

        Assert.Equal(ProTextTextAlignment.Left, InvokeEnum<ProTextTextAlignment>(method, "Start"));
        Assert.Equal(ProTextTextAlignment.Center, InvokeEnum<ProTextTextAlignment>(method, "Center"));
        Assert.Equal(ProTextTextAlignment.Right, InvokeEnum<ProTextTextAlignment>(method, "End"));
    }

    [Fact]
    public void MapsFlowDirectionToCore()
    {
        var method = RequiredMethod(typeof(ProTextFlowDirection), "Microsoft.Maui.FlowDirection",
            "ToCore", "ToCoreFlowDirection", "ToFlowDirection");

        Assert.Equal(ProTextFlowDirection.LeftToRight, InvokeEnum<ProTextFlowDirection>(method, "LeftToRight"));
        Assert.Equal(ProTextFlowDirection.RightToLeft, InvokeEnum<ProTextFlowDirection>(method, "RightToLeft"));
        Assert.Equal(ProTextFlowDirection.LeftToRight, InvokeEnum<ProTextFlowDirection>(method, "MatchParent"));
    }

    [Fact]
    public void MapsTextDecorationsToImmutableCoreSnapshot()
    {
        var method = RequiredMethod(typeof(IReadOnlyList<ProTextDecoration>), "Microsoft.Maui.TextDecorations",
            "SnapshotDecorations");
        var result = InvokeFlags(method, "Underline, Strikethrough");
        var snapshot = Assert.IsAssignableFrom<IReadOnlyList<ProTextDecoration>>(result);

        Assert.Equal(2, snapshot.Count);
        Assert.Contains(snapshot, decoration => decoration.Location == ProTextDecorationLocation.Underline);
        Assert.Contains(snapshot, decoration => decoration.Location == ProTextDecorationLocation.Strikethrough);
    }

    [Fact]
    public void NormalizesMauiLineHeight()
    {
        var method = RequiredMethod(typeof(double), typeof(double),
            "NormalizeLineHeight", "NormalizeMauiLineHeight");

        Assert.True(double.IsNaN((double)method.Invoke(null, [0d])!));
        Assert.True(double.IsNaN((double)method.Invoke(null, [double.PositiveInfinity])!));
        Assert.Equal(1.35, (double)method.Invoke(null, [1.35d])!, precision: 6);
    }

    [Fact]
    public void ConvertsMauiCharacterSpacingToCoreLetterSpacing()
    {
        var method = RequiredMethod(typeof(double), typeof(double), typeof(double),
            "ToLetterSpacing", "ToCoreLetterSpacing");

        Assert.Equal(1.75, (double)method.Invoke(null, [1.25d, 0.5d])!, precision: 6);
    }

    private static MethodInfo RequiredMethod(Type returnType, string parameterTypeName, params string[] names)
    {
        var adapter = ProTextMauiTestAssembly.RequiredAdapterType();
        var method = adapter
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(method =>
                names.Contains(method.Name, StringComparer.Ordinal) &&
                ReturnsAssignableTo(method, returnType) &&
                method.GetParameters() is [{ } parameter] &&
                parameter.ParameterType.FullName == parameterTypeName);

        Assert.NotNull(method);
        return method;
    }

    private static MethodInfo RequiredMethod(Type returnType, Type firstParameterType, params string[] names)
    {
        var adapter = ProTextMauiTestAssembly.RequiredAdapterType();
        var method = adapter
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(method =>
                names.Contains(method.Name, StringComparer.Ordinal) &&
                ReturnsAssignableTo(method, returnType) &&
                method.GetParameters() is [{ } parameter] &&
                parameter.ParameterType == firstParameterType);

        Assert.NotNull(method);
        return method;
    }

    private static MethodInfo RequiredMethod(Type returnType, Type firstParameterType, Type secondParameterType, params string[] names)
    {
        var adapter = ProTextMauiTestAssembly.RequiredAdapterType();
        var method = adapter
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(method =>
                names.Contains(method.Name, StringComparer.Ordinal) &&
                ReturnsAssignableTo(method, returnType) &&
                method.GetParameters() is [{ } firstParameter, { } secondParameter] &&
                firstParameter.ParameterType == firstParameterType &&
                secondParameter.ParameterType == secondParameterType);

        Assert.NotNull(method);
        return method;
    }

    private static bool ReturnsAssignableTo(MethodInfo method, Type returnType)
    {
        return returnType.IsAssignableFrom(method.ReturnType) || method.ReturnType.IsAssignableTo(returnType);
    }

    private static T InvokeEnum<T>(MethodInfo method, string enumName)
    {
        return (T)method.Invoke(null, [Enum.Parse(method.GetParameters()[0].ParameterType, enumName)])!;
    }

    private static object InvokeFlags(MethodInfo method, string enumNames)
    {
        return method.Invoke(null, [Enum.Parse(method.GetParameters()[0].ParameterType, enumNames)])!;
    }
}
