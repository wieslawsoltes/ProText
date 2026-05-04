using System.Globalization;

namespace ProText.Core;

internal static class ProTextFingerprint
{
    public static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
