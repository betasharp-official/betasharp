using System.ComponentModel;
using System.Reflection;
using BetaSharp.Client.Debug.Components;

namespace BetaSharp.Client.Debug;

public static class DebugComponents
{
    public static IEnumerable<Type> RegisteredComponentTypes => s_registry.Values.Select(v => v.Type);

    private static readonly Dictionary<string, (Type Type, Func<DebugComponent> Factory)> s_registry =
        new(StringComparer.OrdinalIgnoreCase);

    public static void Register<T>() where T : DebugComponent, new()
    {
        Type type = typeof(T);
        s_registry[type.Name] = (type, () => new T());
    }

    public static void RegisterComponents()
    {
        Register<DebugVersion>();
        Register<DebugFPS>();
        Register<DebugSeparator>();
        Register<DebugEntities>();
        Register<DebugWorld>();
        Register<DebugParticles>();
        Register<DebugLocation>();
        Register<DebugMemory>();
        Register<DebugFramework>();
        Register<DebugSystem>();
        Register<DebugTargetedBlock>();
        Register<DebugServer>();
    }

    public static string GetName(Type t)
    {
        DisplayNameAttribute? attr = t.GetCustomAttribute<DisplayNameAttribute>();
        return attr?.DisplayName ?? t.Name;
    }

    public static string? GetDescription(Type t)
    {
        return t.GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    public static DebugComponent? CreateFromTypeName(string typeName)
    {
        if (s_registry.TryGetValue(typeName, out (Type Type, Func<DebugComponent> Factory) entry))
        {
            return entry.Factory();
        }

        return null;
    }
}
