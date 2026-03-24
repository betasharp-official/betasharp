using System.ComponentModel;
using System.Reflection;
using BetaSharp.Client.Guis.Debug.Components;

namespace BetaSharp.Client.Guis.Debug;

public static class DebugComponents
{
    public static readonly List<Type> Components = [];

    private static void CheckSubclass(Type t)
    {
        if (!typeof(DebugComponent).IsAssignableFrom(t))
        {
            throw new InvalidOperationException("Type is not a DebugComponent!");
        }
    }
    public static void Register(Type t)
    {
        CheckSubclass(t);
        Components.Add(t);
    }

    public static void RegisterComponents()
    {
        Register(typeof(DebugVersion));
        Register(typeof(DebugFPS));
        Register(typeof(DebugSeparator));
        Register(typeof(DebugEntities));
        Register(typeof(DebugWorld));
        Register(typeof(DebugParticles));
        Register(typeof(DebugLocation));
        Register(typeof(DebugMemory));
        Register(typeof(DebugFramework));
        Register(typeof(DebugSystem));
        Register(typeof(DebugTargetedBlock));
        Register(typeof(DebugServer));
    }
    public static string GetName(Type t)
    {
        CheckSubclass(t);

        DisplayNameAttribute? attr = t.GetCustomAttribute<DisplayNameAttribute>();
        if (attr is null) return t.Name; // default to type name

        return attr.DisplayName;
    }

    public static string? GetDescription(Type t)
    {
        CheckSubclass(t);

        DescriptionAttribute? attr = t.GetCustomAttribute<DescriptionAttribute>();
        if (attr is null) return null;

        return attr.Description;
    }

    public static DebugComponent? CreateInstanceFromTypeName(string typeName)
    {
        foreach (Type t in Components)
        {
            if (t.Name == typeName)
            {
                object? instance = Activator.CreateInstance(t);
                if (instance is DebugComponent dc)
                    return dc;
            }
        }

        return null;
    }
}
