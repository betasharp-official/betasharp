using Microsoft.Extensions.Logging;

namespace BetaSharp.Server;

internal class DedicatedPlayerManager : PlayerManager
{
    private readonly ILogger<DedicatedPlayerManager> _logger = Log.Instance.For<DedicatedPlayerManager>();
    private readonly string _bannedPlayersFile;
    private readonly string _bannedIpsFile;
    private readonly string _operatorsFile;
    private readonly string _whitelistFile;

    public DedicatedPlayerManager(MinecraftServer server) : base(server)
    {
        _bannedPlayersFile = server.GetFilePath("banned-players.txt");
        _bannedIpsFile = server.GetFilePath("banned-ips.txt");
        _operatorsFile = server.GetFilePath("ops.txt");
        _whitelistFile = server.GetFilePath("white-list.txt");

        LoadBannedPlayers();
        LoadBannedIps();
        LoadOperators();
        LoadWhitelist();
        SaveBannedPlayers();
        SaveBannedIps();
        SaveOperators();
        SaveWhitelist();
    }

    protected override void LoadBannedPlayers()
    {
        try
        {
            BannedPlayers.Clear();
            if (File.Exists(_bannedPlayersFile))
            {
                foreach (string line in File.ReadAllLines(_bannedPlayersFile))
                {
                    string trimmed = line.Trim().ToLower();
                    if (trimmed.Length > 0)
                        BannedPlayers.Add(trimmed);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to load ban list: {ex}");
        }
    }

    protected override void SaveBannedPlayers()
    {
        try
        {
            File.WriteAllLines(_bannedPlayersFile, BannedPlayers);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to save ban list: {ex}");
        }
    }

    protected override void LoadBannedIps()
    {
        try
        {
            BannedIps.Clear();
            if (File.Exists(_bannedIpsFile))
            {
                foreach (string line in File.ReadAllLines(_bannedIpsFile))
                {
                    string trimmed = line.Trim().ToLower();
                    if (trimmed.Length > 0)
                        BannedIps.Add(trimmed);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to load ip ban list: {ex}");
        }
    }

    protected override void SaveBannedIps()
    {
        try
        {
            File.WriteAllLines(_bannedIpsFile, BannedIps);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to save ip ban list: {ex}");
        }
    }

    protected override void LoadOperators()
    {
        try
        {
            Ops.Clear();
            if (File.Exists(_operatorsFile))
            {
                foreach (string line in File.ReadAllLines(_operatorsFile))
                {
                    string trimmed = line.Trim().ToLower();
                    if (trimmed.Length > 0)
                        Ops.Add(trimmed);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to load operators list: {ex}");
        }
    }

    protected override void SaveOperators()
    {
        try
        {
            File.WriteAllLines(_operatorsFile, Ops);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to save operators list: {ex}");
        }
    }

    protected override void LoadWhitelist()
    {
        try
        {
            Whitelist.Clear();
            if (File.Exists(_whitelistFile))
            {
                foreach (string line in File.ReadAllLines(_whitelistFile))
                {
                    string trimmed = line.Trim().ToLower();
                    if (trimmed.Length > 0)
                        Whitelist.Add(trimmed);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to load white-list: {ex}");
        }
    }

    protected override void SaveWhitelist()
    {
        try
        {
            File.WriteAllLines(_whitelistFile, Whitelist);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to save white-list: {ex}");
        }
    }
}
