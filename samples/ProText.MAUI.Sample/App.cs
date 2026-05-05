namespace ProText.MAUI.Sample;

public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        UserAppTheme = AppTheme.Light;
        return new Window(new MainPage());
    }
}
