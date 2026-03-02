using Microsoft.Extensions.Logging;

namespace BetaSharp.Server;

internal class DedicatedServerConfiguration : IServerConfiguration
{
    private readonly ILogger<DedicatedServerConfiguration> _logger = Log.Instance.For<DedicatedServerConfiguration>();
    private readonly PropertiesFile _properties = new();
    private readonly string _filePath;

    public DedicatedServerConfiguration(string filePath)
    {
        _filePath = filePath;
        if (File.Exists(filePath))
        {
            try
            {
                _properties.Load(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load {File}", filePath);
                GenerateNew();
            }
        }
        else
        {
            _logger.LogWarning("{File} does not exist", filePath);
            GenerateNew();
        }
    }

    private void GenerateNew()
    {
        _logger.LogInformation("Generating new properties file");
        Save();
    }

    public void Save()
    {
        try
        {
            _properties.Save(_filePath, "Minecraft server properties");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save {File}", _filePath);
        }
    }

    public string GetProperty(string property, string fallback)
    {
        if (!_properties.ContainsKey(property))
        {
            _properties.SetProperty(property, fallback);
            Save();
        }

        return _properties.GetProperty(property, fallback);
    }

    public int GetProperty(string property, int fallback)
    {
        if (!int.TryParse(GetProperty(property, fallback.ToString()), out int result))
        {
            _properties.SetProperty(property, fallback.ToString());
            return fallback;
        }

        return result;
    }

    public bool GetProperty(string property, bool fallback)
    {
        if (!bool.TryParse(GetProperty(property, fallback.ToString().ToLower()), out bool result))
        {
            _properties.SetProperty(property, fallback.ToString().ToLower());
            return fallback;
        }

        return result;
    }

    public void SetProperty(string property, bool value)
    {
        _properties.SetProperty(property, value.ToString().ToLower());
        Save();
    }

    public string GetServerIp(string fallback) => GetProperty("server-ip", fallback);
    public int GetServerPort(int fallback) => GetProperty("server-port", fallback);
    public bool GetDualStack(bool fallback) => GetProperty("dual-stack", fallback);
    public bool GetOnlineMode(bool fallback) => GetProperty("online-mode", fallback);
    public bool GetSpawnAnimals(bool fallback) => GetProperty("spawn-animals", fallback);
    public bool GetPvpEnabled(bool fallback) => GetProperty("pvp", fallback);
    public bool GetAllowFlight(bool fallback) => GetProperty("allow-flight", fallback);
    public string GetLevelName(string fallback) => GetProperty("level-name", fallback);
    public string GetLevelSeed(string fallback) => GetProperty("level-seed", fallback);
    public bool GetSpawnMonsters(bool fallback) => GetProperty("spawn-monsters", fallback);
    public bool GetAllowNether(bool fallback) => GetProperty("allow-nether", fallback);
    public int GetMaxPlayers(int fallback) => GetProperty("max-players", fallback);
    public int GetViewDistance(int fallback) => GetProperty("view-distance", fallback);
    public bool GetWhiteList(bool fallback) => GetProperty("white-list", fallback);
}
