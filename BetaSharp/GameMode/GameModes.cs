using System.Diagnostics.CodeAnalysis;
using BetaSharp.DataAsset;
using Microsoft.Extensions.Logging;

namespace BetaSharp.GameMode;

public static class GameModes
{
    public static GameMode DefaultGameMode { get; private set; } = null!;

    private static readonly ILogger s_logger = Log.Instance.For(nameof(GameModes));

    public static AssetLoader<GameMode> GameModesLoader { get; } = new("gamemode", LoadLocations.AllData);


    public static void SetDefaultGameMode(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            SetDefaultGameMode();
        }
        else if (!TrySetDefaultGameMode(name))
        {
            s_logger.LogError($"SetDefaultGameMode: Gamemode with name {name} not found.");
        }
    }

    public static void SetDefaultGameMode()
    {
        if (TrySetDefaultGameMode("survival")) return;
        if (TrySetDefaultGameMode("default")) return;

        DefaultGameMode = GameModesLoader.Assets.First().Value;
        s_logger.LogWarning($"SetDefaultGameMode: No default gamemode found. using {DefaultGameMode.Name}");
    }

    private static bool TrySetDefaultGameMode(string name)
    {
        if (TryGet(name, out var gameMode, true))
        {
            DefaultGameMode = gameMode;
            return true;
        }

        return false;
    }

    public static GameMode Get(string name, bool shortName = false) =>
        TryGet(name, out var gameMode, shortName) ? gameMode : throw new ArgumentException($"Game mode with name {name} not found.");

    public static bool TryGet(string name, [NotNullWhen(true)] out GameMode? gameMode, bool shortName = false) =>
        GameModesLoader.TryGet(name, out gameMode, shortName);
}
