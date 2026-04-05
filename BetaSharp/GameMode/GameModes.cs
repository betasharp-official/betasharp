using System.Diagnostics.CodeAnalysis;
using BetaSharp.DataAsset;
using BetaSharp.Registries;
using Microsoft.Extensions.Logging;

namespace BetaSharp.GameMode;

public static class GameModes
{
    public static GameMode DefaultGameMode { get; private set; } = null!;

    private static readonly ILogger s_logger = Log.Instance.For(nameof(GameModes));

    public static void SetDefaultGameMode(RegistryAccess registryAccess, string name)
    {
        if (string.IsNullOrEmpty(name))
            SetDefaultGameMode(registryAccess);
        else if (!TrySetDefaultGameMode(registryAccess, name))
            s_logger.LogError($"SetDefaultGameMode: Gamemode with name {name} not found.");
    }

    public static void SetDefaultGameMode(RegistryAccess registryAccess)
    {
        if (TrySetDefaultGameMode(registryAccess, "survival")) return;
        if (TrySetDefaultGameMode(registryAccess, "default")) return;

        var registry = registryAccess.GetOrThrow(RegistryKeys.GameModes);
        ResourceLocation? firstKey = registry.Keys.FirstOrDefault();
        if (firstKey == null)
        {
            s_logger.LogError("SetDefaultGameMode: No game modes are registered.");
            return;
        }

        DefaultGameMode = registry.Get(firstKey)!;
        s_logger.LogWarning($"SetDefaultGameMode: No default gamemode found. using {DefaultGameMode.Name}");
    }

    private static bool TrySetDefaultGameMode(RegistryAccess registryAccess, string name)
    {
        if (TryGet(registryAccess, name, out var gameMode, true))
        {
            DefaultGameMode = gameMode;
            return true;
        }
        return false;
    }

    public static GameMode Get(RegistryAccess registryAccess, string name, bool shortName = false) =>
        TryGet(registryAccess, name, out var gameMode, shortName)
            ? gameMode
            : throw new ArgumentException($"Game mode with name {name} not found.");

    public static bool TryGet(RegistryAccess registryAccess, string name, [NotNullWhen(true)] out GameMode? gameMode, bool shortName = false)
    {
        var loader = (DataAssetLoader<GameMode>)registryAccess.GetOrThrow(RegistryKeys.GameModes);
        return loader.TryGet(name, out gameMode, shortName);
    }
}
