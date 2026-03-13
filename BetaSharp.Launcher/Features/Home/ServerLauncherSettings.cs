namespace BetaSharp.Launcher.Features.Home;

internal readonly record struct ServerLauncherSettings(
    bool OnlineMode,
    int Port,
    bool AllowFlight,
    int MaxPlayers,
    int ViewDistance,
    bool SpawnMonsters);
