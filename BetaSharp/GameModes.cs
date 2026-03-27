using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BetaSharp;

public static class GameModes
{
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
            gameModes.Add(NewSurvivalGameMode());
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


        gameModes.Sort((a, b) => a.Id.CompareTo(b.Id));

        int highestId = gameModes.Last().Id;

        if (highestId != gameModes.Count - 1)
        {
            bool needsResort = false;

            for (int i = gameModes.Count - 1; i >= 0; i--)
            {
                // if id is 0, auto assign id if not name is DefaultName.
                if (gameModes[i].Id < 0)
                {
                    needsResort = true;
                    gameModes[i].Id = ++highestId;
                    s_logger.LogInformation($"Mapped game mode {gameModes[i].Name} to index {i}.");

                    continue;
                }

                if (gameModes[i].Id != gameModes[i - 1].Id) continue;

                s_logger.LogError($"Duplicate game mode ID found: {gameModes[i].Id}. Removing duplicate.");
                gameModes.RemoveAt(i);
            }

            if (needsResort)
            {
                gameModes.Sort((a, b) => a.Id.CompareTo(b.Id));
            }
        }

        s_gameModes = gameModes.ToArray();
    }

    public static GameMode Get(int id) =>
        TryGet(id, out var gameMode) ? gameMode : throw new ArgumentException($"Game mode with ID {id} not found.");

    public static bool TryGet(int id, [NotNullWhen(true)] out GameMode? gameMode)
    {
        foreach (var gm in s_gameModes)
        {
            if (gm.Id != id) continue;

            gameMode = gm;
            return true;
        }

        gameMode = null;
        return false;
    }

    public static GameMode Get(string name) =>
        TryGet(name, out var gameMode) ? gameMode : throw new ArgumentException($"Game mode with name {name} not found.");

    public static bool TryGet(string name, [NotNullWhen(true)] out GameMode? gameMode)
    {
        foreach (var gm in s_gameModes)
        {
            if (gm.Name != name) continue;

            gameMode = gm;
            return true;
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
        Id = 0,
        Name = "survival",
    };

    private static GameMode NewCreativeGameMode() => new()
    {
        Id = 1,
        Name = "creative",
        BrakeSpeed = 0f,
        CanReceiveDamage = false,
        FiniteResources = false,
        CanBeTargeted = false,
        BlockDrops = false,
    };

    private static GameMode NewAdventureGameMode() => new()
    {
        Id = 2,
        Name = "adventure",
        CanBreak = false,
        CanPlace = false,
    };

    private static GameMode NewSpectatorGameMode() => new()
    {
        Id = 3,
        Name = "spectator",
        CanBreak = false,
        CanPlace = false,
        CanInteract = false,
        CanReceiveDamage = false,
        CanInflictDamage = false,
        CanBeTargeted = false,
        CanExhaustFire = false,
        CanPickup =  false,
        VisibleToWorld = false,
    };
}
