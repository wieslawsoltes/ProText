using Microsoft.Maui.Hosting;
using ProText.MAUI.Internal;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace ProText.MAUI;

/// <summary>
/// Registers the SkiaSharp MAUI handlers used by ProText controls.
/// </summary>
public static class ProTextMauiAppBuilderExtensions
{
    /// <summary>
    /// Adds the MAUI SkiaSharp handlers required by the internal ProText drawing surface.
    /// </summary>
    public static MauiAppBuilder UseProTextMaui(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ProTextMauiPlatform.EnsureConfigured();
        builder.UseSkiaSharp();
        return builder;
    }
}
