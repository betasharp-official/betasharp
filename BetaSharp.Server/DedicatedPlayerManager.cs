using java.io;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server;

internal class DedicatedPlayerManager : PlayerManager
{
    private readonly ILogger<DedicatedPlayerManager> _logger = Log.Instance.For<DedicatedPlayerManager>();
    private readonly java.io.File BANNED_PLAYERS_FILE;
    private readonly java.io.File BANNED_IPS_FILE;
    private readonly java.io.File OPERATORS_FILE;
    private readonly java.io.File WHITELIST_FILE;

    public DedicatedPlayerManager(BetaSharpServer server) : base(server)
    {
        BANNED_PLAYERS_FILE = server.getFile("banned-players.txt");
        BANNED_IPS_FILE = server.getFile("banned-ips.txt");
        OPERATORS_FILE = server.getFile("ops.txt");
        WHITELIST_FILE = server.getFile("white-list.txt");

        loadBannedPlayers();
        loadBannedIps();
        loadOperators();
        loadWhitelist();
        saveBannedPlayers();
        saveBannedIps();
        saveOperators();
        saveWhitelist();
    }

    protected override void loadBannedPlayers()
    {
        try
        {
            bannedPlayers.Clear();
            BufferedReader var1 = new(new FileReader(BANNED_PLAYERS_FILE));
            string var2 = "";

            while ((var2 = var1.readLine()) != null)
            {
                bannedPlayers.Add(var2.Trim().ToLower());
            }

            var1.close();
        }
        catch (Exception var3)
        {
            _logger.LogWarning($"Failed to load ban list: {var3}");
        }
    }

    protected override void saveBannedPlayers()
    {
        try
        {
            PrintWriter var1 = new(new FileWriter(BANNED_PLAYERS_FILE, false));

            foreach (string var3 in bannedPlayers)
            {
                var1.println(var3);
            }

            var1.close();
        }
        catch (Exception var4)
        {
            _logger.LogWarning($"Failed to save ban list: {var4}");
        }
    }

    protected override void loadBannedIps()
    {
        try
        {
            bannedIps.Clear();
            BufferedReader var1 = new(new FileReader(BANNED_IPS_FILE));
            string var2 = "";

            while ((var2 = var1.readLine()) != null)
            {
                bannedIps.Add(var2.Trim().ToLower());
            }

            var1.close();
        }
        catch (Exception var3)
        {
            _logger.LogWarning($"Failed to load ip ban list: {var3}");
        }
    }

    protected override void saveBannedIps()
    {
        try
        {
            PrintWriter var1 = new(new FileWriter(BANNED_IPS_FILE, false));

            foreach (string var3 in bannedIps)
            {
                var1.println(var3);
            }

            var1.close();
        }
        catch (Exception var4)
        {
            _logger.LogWarning($"Failed to save ip ban list: {var4}");
        }
    }

    protected override void loadOperators()
    {
        try
        {
            ops.Clear();
            BufferedReader var1 = new(new FileReader(OPERATORS_FILE));
            string var2 = "";

            while ((var2 = var1.readLine()) != null)
            {
                ops.Add(var2.Trim().ToLower());
            }

            var1.close();
        }
        catch (Exception var3)
        {
            _logger.LogWarning($"Failed to load ip ban list: {var3}");
        }
    }

    protected override void saveOperators()
    {
        try
        {
            PrintWriter var1 = new(new FileWriter(OPERATORS_FILE, false));

            foreach (string var3 in ops)
            {
                var1.println(var3);
            }

            var1.close();
        }
        catch (Exception var4)
        {
            _logger.LogWarning($"Failed to save ip ban list: {var4}");
        }
    }

    protected override void loadWhitelist()
    {
        try
        {
            whitelist.Clear();
            BufferedReader var1 = new(new FileReader(WHITELIST_FILE));
            string var2 = "";

            while ((var2 = var1.readLine()) != null)
            {
                whitelist.Add(var2.Trim().ToLower());
            }

            var1.close();
        }
        catch (Exception var3)
        {
            _logger.LogWarning($"Failed to load white-list: {var3}");
        }
    }

    protected override void saveWhitelist()
    {
        try
        {
            PrintWriter var1 = new(new FileWriter(WHITELIST_FILE, false));

            foreach (String var3 in whitelist)
            {
                var1.println(var3);
            }

            var1.close();
        }
        catch (Exception var4)
        {
            _logger.LogWarning($"Failed to save white-list: {var4}");
        }
    }
}
