using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BetaSharp;

public static class GameModes
{
    public static GameMode DefaultGameMode { get; private set; } = null!;

    private static readonly ILogger s_logger = Log.Instance.For(nameof(GameModes));
    // ReSharper disable once StringLiteralTypo
    private static readonly ResourceLocation s_location = new(ResourceLocation.DefaultNamespace, "gamemode");
    private static readonly GameMode[] s_gameModes;

    static GameModes()
    {
        List<GameMode> gameModes = new(4);
        string path = Path.Join("assets", s_location.Path);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        JsonSerializerOptions options = new()
        {
            RespectRequiredConstructorParameters = true,
            WriteIndented = true,

        };

        foreach (string file in Directory.EnumerateFiles(path, "*.json"))
        {
            string json = File.ReadAllText(file);
            var g = JsonSerializer.Deserialize<List<GameMode>>(json, options);
            if (g != null)
                gameModes.AddRange(g);
        }

        if (gameModes.Count == 0)
        {
            s_logger.LogError("No game Modes found, adding default game modes.");
            gameModes.Add(DefaultGameMode = NewSurvivalGameMode());
            gameModes.Add(NewCreativeGameMode());
            gameModes.Add(NewAdventureGameMode());
            gameModes.Add(NewSpectatorGameMode());
            s_gameModes = gameModes.ToArray();

            foreach (GameMode gm in s_gameModes)
            {
                File.WriteAllText(Path.Join(path, gm.Name + ".json"), JsonSerializer.Serialize(new[] { gm }, options));
            }

            return;
        }

        // remove duplicates
        for (int i = 0; i <= gameModes.Count-2; i++)
        {
            for (int j = gameModes.Count-1; j > i; j--)
            {
                if (gameModes[i].Name == gameModes[j].Name)
                {
                    s_logger.LogError($"Duplicate gamemode found: {gameModes[j].Name}. Removing duplicate.");
                    gameModes.RemoveAt(j);
                    break;
                }
            }
        }

        s_gameModes = gameModes.ToArray();
        SetDefaultGameMode();
    }

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

        DefaultGameMode = Get(0);
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

    public static GameMode Get(int id) =>
        TryGet(id, out var gameMode) ? gameMode : throw new ArgumentException($"Game mode with ID {id} not found.");

    public static bool TryGet(int id, [NotNullWhen(true)] out GameMode? gameMode)
    {
        if (id >= 0 && s_gameModes.Length > id)
        {
            gameMode = s_gameModes[id];
            return true;
        }

        gameMode = null;
        return false;
    }

    public static GameMode Get(string name, bool shortName = false) =>
        TryGet(name, out var gameMode, shortName) ? gameMode : throw new ArgumentException($"Game mode with name {name} not found.");

    public static bool TryGet(string name, [NotNullWhen(true)] out GameMode? gameMode, bool shortName = false)
    {
        foreach (var gm in s_gameModes)
        {
            if (gm.Name != name) continue;

            gameMode = gm;
            return true;
        }

        if (shortName)
        {
            int nameLen = name.Length;

            if (nameLen == 1) return TryGet(name[0], out gameMode);

            foreach (var gm in s_gameModes)
            {
                if (gm.Name.Length <= nameLen || gm.Name.Substring(0, nameLen) != name) continue;

                gameMode = gm;
                return true;
            }
        }

        gameMode = null;
        return false;
    }

    public static GameMode Get(char name) =>
        TryGet(name, out var gameMode) ? gameMode : throw new ArgumentException($"Game mode with name {name} not found.");

    public static bool TryGet(char name, [NotNullWhen(true)] out GameMode? gameMode)
    {
        foreach (var gm in s_gameModes)
        {
            if (gm.Name[0] != name) continue;

            gameMode = gm;
            return true;
        }

        gameMode = null;
        return false;
    }

    private static GameMode NewSurvivalGameMode() => new()
    {
        Name = "survival",
    };

    private static GameMode NewCreativeGameMode() => new()
    {
        Name = "creative",
        BrakeSpeed = 0f,
        CanReceiveDamage = false,
        FiniteResources = false,
        CanBeTargeted = false,
        BlockDrops = false,
    };

    private static GameMode NewAdventureGameMode() => new()
    {
        Name = "adventure",
        CanBreak = false,
        CanPlace = false,
    };

    private static GameMode NewSpectatorGameMode() => new()
    {
        Name = "spectator",
        CanBreak = false,
        CanPlace = false,
        CanInteract = false,
        CanReceiveDamage = false,
        CanInflictDamage = false,
        CanBeTargeted = false,
        CanExhaustFire = false,
        CanPickup =  false,
        CanDrop =  false,
        VisibleToWorld = false,
    };
}
