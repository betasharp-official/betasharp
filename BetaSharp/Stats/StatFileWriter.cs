using System.Text;
using System.Text.Json;
using BetaSharp.Util;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Stats;

public class StatFileWriter
{
    private static readonly ILogger<StatFileWriter> s_logger = Log.Instance.For<StatFileWriter>();

    private readonly Dictionary<StatBase, int> _statsData = new();
    private readonly Dictionary<StatBase, int> _statsSyncedData = new();
    private bool _statsExist;
    private readonly StatsSynchronizer _statsSyncer;

    public StatFileWriter(Session session, string mcDataDir)
    {
        string statsFolder = System.IO.Path.Combine(mcDataDir, "stats");
        if (!Directory.Exists(statsFolder))
        {
            Directory.CreateDirectory(statsFolder);
        }

        if (Directory.Exists(mcDataDir))
        {
            foreach (string filePath in Directory.GetFiles(mcDataDir, "stats_*.dat"))
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                string targetPath = System.IO.Path.Combine(statsFolder, fileName);

                if (!File.Exists(targetPath))
                {
                    s_logger.LogInformation($"Relocating {fileName}");
                    File.Move(filePath, targetPath);
                }
            }
        }
        _statsSyncer = new StatsSynchronizer(session, this, statsFolder);
    }

    public void ReadStat(StatBase stat, int increment)
    {
        WriteStatToMap(_statsSyncedData, stat, increment);
        WriteStatToMap(_statsData, stat, increment);
        _statsExist = true;
    }

    private static void WriteStatToMap(Dictionary<StatBase, int> map, StatBase stat, int increment)
    {
        map.TryGetValue(stat, out int current);
        map[stat] = current + increment;
    }

    public Dictionary<StatBase, int> GetStatsSyncedData()
    {
        return new Dictionary<StatBase, int>(_statsSyncedData);
    }

    public void LoadStats(Dictionary<StatBase, int> statsMap)
    {
        if (statsMap != null)
        {
            _statsExist = true;
            foreach (var kvp in statsMap)
            {
                WriteStatToMap(_statsSyncedData, kvp.Key, kvp.Value);
                WriteStatToMap(_statsData, kvp.Key, kvp.Value);
            }
        }
    }

    public void AddStats(Dictionary<StatBase, int> newStats)
    {
        if (newStats != null)
        {
            foreach (var kvp in newStats)
            {
                _statsSyncedData.TryGetValue(kvp.Key, out int currentSynced);
                _statsData[kvp.Key] = kvp.Value + currentSynced;
            }
        }
    }

    public void SetStats(Dictionary<StatBase, int> newStats)
    {
        if (newStats != null)
        {
            _statsExist = true;
            foreach (var kvp in newStats)
            {
                WriteStatToMap(_statsSyncedData, kvp.Key, kvp.Value);
            }
        }
    }

    public static Dictionary<StatBase, int> CreateStatsMap(string statsFileContents)
    {
        var statsMap = new Dictionary<StatBase, int>();
        try
        {
            StringBuilder sb = new StringBuilder();

            using JsonDocument statsJson = JsonDocument.Parse(statsFileContents);
            JsonElement root = statsJson.RootElement;

            if (root.TryGetProperty("stats-change", out JsonElement statsChangeArray))
            {
                foreach (JsonElement statJson in statsChangeArray.EnumerateArray())
                {
                    JsonProperty prop = statJson.EnumerateObject().First();

                    int id = int.Parse(prop.Name);
                    int value = prop.Value.ValueKind == JsonValueKind.Number
                        ? prop.Value.GetInt32()
                        : int.Parse(prop.Value.GetString() ?? "0");

                    StatBase statBase = Stats.GetStatById(id);
                    if (statBase == null)
                    {
                        s_logger.LogInformation($"{id} is not a valid stat");
                    }
                    else
                    {
                        sb.Append(statBase.StatGuid).Append(",");
                        sb.Append(value).Append(",");
                        statsMap[statBase] = value;
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            s_logger.LogError(ex, "Exception");
        }

        return statsMap;
    }

    public static string SerializeStats(string username, string salt, Dictionary<StatBase, int> statsMap)
    {
        var sb = new StringBuilder();
        bool isFirst = true;

        sb.Append("{\r\n");
        if (username != null && salt != null)
        {
            sb.Append("  \"user\":{\r\n");
            sb.Append("    \"name\":\"").Append(username).Append("\",\r\n");
            sb.Append("    \"sessionid\":\"").Append(salt).Append("\"\r\n");
            sb.Append("  },\r\n");
        }

        sb.Append("  \"stats-change\":[");
        var hashDataBuilder = new StringBuilder();

        foreach (var kvp in statsMap)
        {
            StatBase stat = kvp.Key;
            int value = kvp.Value;

            if (!isFirst)
                sb.Append("},");
            else
                isFirst = false;

            sb.Append("\r\n    {\"").Append(stat.Id).Append("\":").Append(value);

            hashDataBuilder.Append(stat.StatGuid).Append(",");
            hashDataBuilder.Append(value).Append(",");
        }

        if (!isFirst)
            sb.Append("}");

        sb.Append("\r\n  ],\r\n");
        sb.Append("  \"checksum\":\"").Append("NoChecksum").Append("\"\r\n");
        sb.Append("}");

        return sb.ToString();
    }

    public bool HasAchievementUnlocked(Achievement achievement)
    {
        return _statsData.ContainsKey(achievement);
    }

    public bool CanUnlockAchievement(Achievement achievement)
    {
        return achievement.parent == null || HasAchievementUnlocked(achievement.parent);
    }

    public int GetStatValue(StatBase stat)
    {
        return _statsData.TryGetValue(stat, out int val) ? val : 0;
    }

    public static void Tick()
    {
    }

    public void SyncStats()
    {
        _statsSyncer.SyncStatsFileWithMap(GetStatsSyncedData());
    }

    public void SyncStatsIfReady()
    {
        if (_statsExist && _statsSyncer.IsReadyToSync())
        {
            _statsSyncer.SendStats(GetStatsSyncedData());
        }

        _statsSyncer.Tick();
    }
}
