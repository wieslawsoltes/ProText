using System.Reflection;

namespace ProText.MAUI.Tests;

internal static class ProTextMauiTestAssembly
{
    private const string AssemblyName = "ProText.MAUI";
    private static readonly Lazy<Assembly> s_assembly = new(LoadAssembly);

    public static Assembly Assembly => s_assembly.Value;

    public static Type RequiredType(string fullName)
    {
        return Assembly.GetType(fullName, throwOnError: true)!;
    }

    public static Type RequiredAdapterType()
    {
        return RequiredType("ProText.MAUI.Internal.ProTextMauiAdapter");
    }

    public static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ProText.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root.");
    }

    private static Assembly LoadAssembly()
    {
        try
        {
            return Assembly.Load(new AssemblyName(AssemblyName));
        }
        catch (FileNotFoundException)
        {
            var assemblyPath = Path.Combine(AppContext.BaseDirectory, $"{AssemblyName}.dll");
            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }

            throw new FileNotFoundException(
                "ProText.MAUI.dll was not found. Add src/ProText.MAUI/ProText.MAUI.csproj or build it before running ProText.MAUI.Tests.",
                assemblyPath);
        }
    }
}
