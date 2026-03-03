namespace BetaSharp.Server.Internal;

internal class InternalServerConfiguration : IServerConfiguration
{
    private string levelName;
    private string seed;
    private int viewDistance;

    public InternalServerConfiguration(string levelName, string seed, int viewDistance)
    {
        this.levelName = levelName;
        this.seed = seed;
        this.viewDistance = viewDistance;
    }

    public void SetViewDistance(int distance)
    {
        viewDistance = distance;
    }

    public bool GetAllowFlight(bool fallback)
    {
        return true;
    }

    public bool GetAllowNether(bool fallback)
    {
        return true;
    }

    public string GetLevelName(string fallback)
    {
        return levelName;
    }

    public string GetLevelSeed(string fallback)
    {
        return seed;
    }

    public int GetMaxPlayers(int fallback)
    {
        return 1;
    }

    public bool GetOnlineMode(bool fallback)
    {
        return false;
    }

    public bool GetProperty(string property, bool fallback)
    {
        return false;
    }

    public int GetProperty(string property, int fallback)
    {
        return -1;
    }

    public string GetProperty(string property, string fallback)
    {
        return string.Empty;
    }

    public bool GetPvpEnabled(bool fallback)
    {
        return false;
    }

    public string GetServerIp(string fallback)
    {
        return "";
    }

    public bool GetDualStack(bool fallback)
    {
        return false;
    }

    public int GetServerPort(int fallback)
    {
        return 25565;
    }

    public bool GetSpawnAnimals(bool fallback)
    {
        return true;
    }

    public bool GetSpawnMonsters(bool fallback)
    {
        return true;
    }

    public int GetViewDistance(int fallback)
    {
        return viewDistance;
    }

    public bool GetWhiteList(bool fallback)
    {
        return false;
    }

    public int GetSpawnRegionSize(int fallback)
    {
        return fallback;
    }

    public void Save()
    {
    }

    public void SetProperty(string property, bool value)
    {
    }
}
