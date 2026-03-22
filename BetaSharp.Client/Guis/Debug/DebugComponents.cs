using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using BetaSharp.Client.Guis.Debug.Components;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Guis.Debug;
public static class DebugComponents
{
    public static readonly List<Type> Components
        = new List<Type>();

    private static void checkSubclass(Type t)
    {
        if (!typeof(DebugComponent).IsAssignableFrom(t))
        {
            throw new InvalidOperationException("Type is not a DebugComponent!");
        }
    } 
    public static void Register(Type t)
    {
        checkSubclass(t);
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
    }
    public static string GetName(Type t)
    {
        checkSubclass(t);

        DisplayNameAttribute? attr = t.GetCustomAttribute<DisplayNameAttribute>();
        if (attr is null) return t.Name; // just default to type name

        return attr.DisplayName;
    }

    public static string? GetDescription(Type t)
    {
        checkSubclass(t);

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
                var instance = Activator.CreateInstance(t);
                if (instance is DebugComponent dc)
                    return dc;
            }
        }

        return null;
    }
}
