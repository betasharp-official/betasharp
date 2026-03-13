using System;
using System.Collections.Generic;
using System.IO;

namespace BetaSharp.Launcher.Features.Home;

internal sealed class ServerPropertiesService
{
    public const bool DefaultOnlineMode = false;
    public const int DefaultPort = 25565;
    public const bool DefaultAllowFlight = false;
    public const int DefaultMaxPlayers = 20;
    public const int DefaultViewDistance = 10;
    public const bool DefaultSpawnMonsters = true;

    private const string OnlineModeKey = "online-mode";
    private const string PortKey = "server-port";
    private const string AllowFlightKey = "allow-flight";
    private const string MaxPlayersKey = "max-players";
    private const string ViewDistanceKey = "view-distance";
    private const string SpawnMonstersKey = "spawn-monsters";

    private readonly string _serverDirectory = Path.Combine(AppContext.BaseDirectory, "Server");

    public string ServerPropertiesPath => Path.Combine(_serverDirectory, "server.properties");

    public ServerLauncherSettings Load()
    {
        var defaults = new ServerLauncherSettings(
            DefaultOnlineMode,
            DefaultPort,
            DefaultAllowFlight,
            DefaultMaxPlayers,
            DefaultViewDistance,
            DefaultSpawnMonsters);

        if (!File.Exists(ServerPropertiesPath))
        {
            return defaults;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string line in File.ReadLines(ServerPropertiesPath))
        {
            if (!TryParsePropertyLine(line, out string key, out string value))
            {
                continue;
            }

            values[key] = value;
        }

        return new ServerLauncherSettings(
            ParseBool(values, OnlineModeKey, defaults.OnlineMode),
            ParseInt(values, PortKey, defaults.Port),
            ParseBool(values, AllowFlightKey, defaults.AllowFlight),
            ParseInt(values, MaxPlayersKey, defaults.MaxPlayers),
            ParseInt(values, ViewDistanceKey, defaults.ViewDistance),
            ParseBool(values, SpawnMonstersKey, defaults.SpawnMonsters));
    }

    public void Save(ServerLauncherSettings settings)
    {
        Directory.CreateDirectory(_serverDirectory);

        var lines = File.Exists(ServerPropertiesPath)
            ? new List<string>(File.ReadAllLines(ServerPropertiesPath))
            : [];

        if (lines.Count == 0)
        {
            lines.Add("#BetaSharp server properties");
        }

        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [OnlineModeKey] = FormatBool(settings.OnlineMode),
            [PortKey] = settings.Port.ToString(),
            [AllowFlightKey] = FormatBool(settings.AllowFlight),
            [MaxPlayersKey] = settings.MaxPlayers.ToString(),
            [ViewDistanceKey] = settings.ViewDistance.ToString(),
            [SpawnMonstersKey] = FormatBool(settings.SpawnMonsters)
        };

        var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < lines.Count; index++)
        {
            if (!TryParsePropertyLine(lines[index], out string key, out _))
            {
                continue;
            }

            if (!updates.TryGetValue(key, out string? value))
            {
                continue;
            }

            lines[index] = $"{key}={value}";
            written.Add(key);
        }

        foreach (var update in updates)
        {
            if (written.Contains(update.Key))
            {
                continue;
            }

            lines.Add($"{update.Key}={update.Value}");
        }

        File.WriteAllLines(ServerPropertiesPath, lines);
    }

    private static bool TryParsePropertyLine(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        string trimmed = line.Trim();
        if (trimmed.StartsWith('#') || trimmed.StartsWith('!'))
        {
            return false;
        }

        int delimiterIndex = line.IndexOf('=');
        if (delimiterIndex < 0)
        {
            delimiterIndex = line.IndexOf(':');
        }

        if (delimiterIndex <= 0)
        {
            return false;
        }

        key = line[..delimiterIndex].Trim();
        value = line[(delimiterIndex + 1)..].Trim();
        return !string.IsNullOrWhiteSpace(key);
    }

    private static bool ParseBool(IReadOnlyDictionary<string, string> values, string key, bool fallback)
    {
        return values.TryGetValue(key, out string? raw) && bool.TryParse(raw, out bool parsed)
            ? parsed
            : fallback;
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
    {
        return values.TryGetValue(key, out string? raw) && int.TryParse(raw, out int parsed)
            ? parsed
            : fallback;
    }

    private static string FormatBool(bool value) => value ? "true" : "false";
}
