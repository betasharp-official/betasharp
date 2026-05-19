namespace BetaSharp.Tests.E2E;

public class PlayerJoinE2ETests
{
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Headless")]
    public void PlayerCanJoinInternalServer()
    {
        if (!HeadlessE2ETestSession.IsEnvironmentReady(out _))
        {
            return;
        }

        using var session = new HeadlessE2ETestSession(nameof(PlayerCanJoinInternalServer));

        HeadlessJoinClientHandler client = session.ConnectClient("e2e-player");

        bool joined = session.WaitUntil(
            () => client.LoginHelloReceived && session.Server.playerManager.players.Count == 1,
            TimeSpan.FromSeconds(20));

        Assert.True(joined, "Client did not finish join flow in time.");
        Assert.True(client.HandshakeReceived);
        Assert.False(client.Disconnected, $"Client unexpectedly disconnected: {client.DisconnectReason}");
        Assert.Single(session.Server.playerManager.players);
    }
}
