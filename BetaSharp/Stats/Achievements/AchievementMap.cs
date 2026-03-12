using Microsoft.Extensions.Logging;
using Exception = System.Exception;
using StringReader = System.IO.StringReader;

namespace BetaSharp.Stats.Achievements;

internal static class AchievementMap
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(AchievementMap));
    private static readonly Dictionary<int, string> s_guidMap = new();

    static AchievementMap()
    {
        try
        {
            using (var reader = new StringReader(AssetManager.Instance.getAsset("achievement/map.txt").GetTextContent()))
            {
                while (reader.ReadLine() is { } line)
                {
                    if (line == "") continue;
                    string[] parts = line.Split(',');
                    int key = int.Parse(parts[0]);
                    s_guidMap.Add(key, parts[1].Trim());
                }
            }
        }
        catch (Exception ex)
        {
            s_logger.LogError(ex, ex.Message);
        }
    }

    public static string GetGuid(int id)
    {
        if (!s_guidMap.TryGetValue(id, out string? value))
        {
            //s_logger.LogWarning("No guid found for id: " + id);
            return string.Empty;
        }

        return value;
    }
}
