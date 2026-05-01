using System.Diagnostics;
using System.Reflection;
using BetaSharp.Client;
using HarmonyLib;

namespace BetaSharp.Client.Modding;

public abstract class Mod
{
    public abstract string ID { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Author { get; }
    public abstract bool AddNamespace { get; }

    public static BetaSharp Game { get; internal set; }
    protected Harmony HarmonyInstance { get; private set; }
    protected Namespace Namespace { get; private set; }

    internal void ApplyPatches()
    {
        HarmonyInstance = new Harmony("com.betasharp.mod." + ID);

        foreach (var type in this.GetType().Assembly.GetTypes())
        {
            var attr = type.GetCustomAttribute<HarmonyPatch>();
            if (attr != null) Debug.WriteLine($"[ModLoader] Found patch class: {type.Name}");
        }

        HarmonyInstance.PatchAll(this.GetType().Assembly);
    }

    internal void InternalInit(BetaSharp game)
    {
        Game = game;
        if (AddNamespace) Namespace = Namespace.Add(ID);

        Init();
    }

    public abstract void Init();
}
