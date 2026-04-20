using System.Diagnostics;
using BetaSharp.Network;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Registries;
using BetaSharp.Server.Internal;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Tests.E2E;

internal sealed class HeadlessE2ETestSession : IDisposable
{
    private static readonly Lock s_runtimeLock = new();
    private static bool s_runtimeInitialized;

    private readonly List<InternalConnection> _clientConnections = [];
    private bool _disposed;

    public string RootPath { get; }
    public InternalServer Server { get; }

    public static bool IsEnvironmentReady(out string reason)
    {
        string jarPath = Path.Combine(AppContext.BaseDirectory, "b1.7.3.jar");
        if (!File.Exists(jarPath))
        {
            reason = $"Missing required test asset: {jarPath}";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public HeadlessE2ETestSession(string? testName = null)
    {
        EnsureRuntimeInitialized();

        string sessionName = string.IsNullOrWhiteSpace(testName) ? "e2e" : testName;
        RootPath = Path.Combine(Path.GetTempPath(), $"BetaSharp-E2E-{sessionName}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(RootPath);

        var worldSettings = new WorldSettings(seed: 1234L, terrainType: WorldType.Default);
        Server = new InternalServer(RootPath, "e2e-world", worldSettings, viewDistance: 2, initialDifficulty: 1)
        {
            RegistryAccess = RegistryAccess.Build(datapackPath: ".")
        };

        Server.RunThreaded("E2E Internal Server");

        bool started = WaitUntil(() => Server.isReady, TimeSpan.FromSeconds(30));
        if (!started)
        {
            throw new TimeoutException("Internal server did not become ready in time.");
        }
    }

    public HeadlessJoinClientHandler ConnectClient(string username)
    {
        var clientConnection = new InternalConnection(netHandler: null, name: $"E2E-Client-{username}");
        var serverConnection = new InternalConnection(netHandler: null, name: $"E2E-Server-{username}");

        clientConnection.AssignRemote(serverConnection);
        serverConnection.AssignRemote(clientConnection);

        var handler = new HeadlessJoinClientHandler(clientConnection, username);
        clientConnection.setNetworkHandler(handler);

        _clientConnections.Add(clientConnection);
        Server.connections.AddInternalConnection(serverConnection);

        // Kick off the standard login handshake.
        clientConnection.sendPacket(new HandshakePacket(username));

        return handler;
    }

    public bool WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            PumpClientsOnce();
            if (condition())
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return false;
    }

    public void PumpClientsOnce()
    {
        foreach (InternalConnection connection in _clientConnections)
        {
            connection.tick();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (InternalConnection connection in _clientConnections)
        {
            connection.disconnect("E2E session shutdown");
            connection.tick();
        }

        Server.Stop();
        WaitUntil(() => Server.stopped, TimeSpan.FromSeconds(5));

        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch
        {
            // Cleanup best effort only.
        }
    }

    private static void EnsureRuntimeInitialized()
    {
        lock (s_runtimeLock)
        {
            if (s_runtimeInitialized)
            {
                return;
            }

            string logRoot = Path.Combine(Path.GetTempPath(), "BetaSharp-E2E-Logs");
            Directory.CreateDirectory(logRoot);

            Log.Instance.Initialize(logRoot);
            AssetManager.Initialize(AssetManager.AssetProfile.Headless);
            Bootstrap.Initialize();

            s_runtimeInitialized = true;
        }
    }
}

internal sealed class HeadlessJoinClientHandler(InternalConnection connection, string username) : NetHandler
{
    public bool HandshakeReceived { get; private set; }
    public bool LoginHelloReceived { get; private set; }
    public bool Disconnected { get; private set; }
    public string? DisconnectReason { get; private set; }

    public override void onHandshake(HandshakePacket packet)
    {
        HandshakeReceived = true;
        connection.sendPacket(new LoginHelloPacket(username, 14, LoginHelloPacket.BETASHARP_CLIENT_SIGNATURE, 0));
    }

    public override void onHello(LoginHelloPacket packet)
    {
        LoginHelloReceived = true;
    }

    public override void onDisconnected(string reason, object[]? details)
    {
        Disconnected = true;
        DisconnectReason = reason;
    }

    public override void onDisconnect(DisconnectPacket packet)
    {
        Disconnected = true;
        DisconnectReason = packet.reason;
    }

    public override bool isServerSide() => false;
}
