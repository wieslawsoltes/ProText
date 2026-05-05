namespace ProText.MAUI.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        ProTextMauiScaffold.ConfigureBuilder(builder);
        return builder.Build();
    }
}
