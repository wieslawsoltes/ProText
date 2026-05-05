using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;

namespace ProText.Uno.Internal;

internal static class ProTextUnoInlineCollectionFactory
{
    public static InlineCollection Create(DependencyObject owner)
    {
        var constructor = typeof(InlineCollection)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(static ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(typeof(DependencyObject));
            });

        if (constructor is not null)
        {
            return (InlineCollection)constructor.Invoke([owner]);
        }

        constructor = typeof(InlineCollection)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(owner);
            });

        if (constructor is not null)
        {
            return (InlineCollection)constructor.Invoke([owner]);
        }

        throw new InvalidOperationException("Unable to create a Uno InlineCollection for ProText.");
    }
}
