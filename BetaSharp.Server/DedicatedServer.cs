using System.IO;
using System.Net;
using System.Net.Sockets;
using BetaSharp.Server.Network;
using BetaSharp.Server.Threading;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server;

internal class DedicatedServer(IServerConfiguration Config) : MinecraftServer(Config)
{
    private static readonly ILogger<DedicatedServer> s_logger = Log.Instance.For<DedicatedServer>();

    protected override PlayerManager CreatePlayerManager()
    {
        return new DedicatedPlayerManager(this);
    }

    protected override bool Init()
    {
        ConsoleInputThread var1 = new(this);
        var1.Start();

        s_logger.LogInformation("Starting minecraft server version Beta 1.7.3");
        if (GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024L / 1024L < 512L)
        {
            s_logger.LogWarning("**** NOT ENOUGH RAM!");
            s_logger.LogWarning("To start the server with more ram, launch it as \"java -Xmx1024M -Xms1024M -jar minecraft_server.jar\"");
        }

        s_logger.LogInformation("Loading properties");

        string addressInput = Config.GetServerIp("");

        bool dualStack = Config.GetDualStack(false);

        var address = dualStack ? IPAddress.IPv6Any : IPAddress.Any;

        if (addressInput.Length > 0)
        {
            address = Dns.GetHostAddresses(addressInput)[0];
        }

        int port = Config.GetServerPort(25565);
        s_logger.LogInformation($"Starting Minecraft server on {(addressInput.Length == 0 ? "*" : addressInput)}:{port}");

        try
        {
            Connections = new ConnectionListener(this, address, port, dualStack);
        }
        catch (IOException ex)
        {
            s_logger.LogWarning("**** FAILED TO BIND TO PORT!");
            s_logger.LogWarning($"The exception was: {ex}");
            s_logger.LogWarning("Perhaps a server is already Running on that port?");
            return false;
        }

        if (!OnlineMode)
        {
            s_logger.LogWarning("**** SERVER IS RUNNING IN OFFLINE/INSECURE MODE!");
            s_logger.LogWarning("The server will make no attempt to authenticate usernames. Beware.");
            s_logger.LogWarning("While this makes the game possible to play without internet access, it also opens up the ability for hackers to connect with any username they choose.");
            s_logger.LogWarning("To change this, set \"online-mode\" to \"true\" in the server.settings file.");
        }

        return base.Init();
    }

    public static void Main(string[] args)
    {
        Log.Instance.Initialize(Directory.GetCurrentDirectory());

        try
        {
            DedicatedServerConfiguration Config = new("server.properties");
            DedicatedServer server = new(Config);

            new RunServerThread(server, "Server thread").Start();
        }
        catch (Exception e)
        {
            s_logger.LogError($"Failed to start the minecraft server: {e}");
        }
    }

    public override string GetFilePath(string path)
    {
        return Path.GetFullPath(path);
    }
}
