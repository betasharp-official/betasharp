namespace BetaSharp.Server.Internal;

internal class InternalServerConfiguration : IServerConfiguration
{
    private readonly string _levelName;
    private readonly string _levelType;
    private readonly string _seed;
    private readonly string _levelOptions;
    private int _viewDistance;

    public InternalServerConfiguration(string levelName, string levelType, string seed, string levelOptions, int viewDistance)
    {
        _levelName = levelName;
        _levelType = levelType;
        _seed = seed;
        _levelOptions = levelOptions;
        _viewDistance = viewDistance;
    }

    public void SetViewDistance(int distance)
    {
        _viewDistance = distance;
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
        return _levelName;
    }

    public string GetLevelType(string fallback)
    {
        return _levelType ?? fallback;
    }

    public string GetLevelSeed(string fallback)
    {
        return _seed;
    }

    public string GetLevelOptions(string fallback)
    {
        return _levelOptions ?? fallback;
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
        return _viewDistance;
    }

    public bool GetWhiteList(bool fallback)
    {
        return false;
    }

    public int GetSpawnRegionSize(int fallback)
    {
        return fallback;
    }

    public string GetDefaultGamemode(string fallback) => fallback;

    public void Save()
    {
    }

    public void SetProperty(string property, bool value)
    {
    }
}
