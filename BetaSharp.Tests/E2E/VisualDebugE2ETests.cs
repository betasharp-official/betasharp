using BetaSharp.Client.UI.Screens.Menu;

namespace BetaSharp.Tests.E2E;

public class VisualDebugE2ETests
{
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Visual")]
    public void VisualLaunchStartsWithDebugUiEnabled()
    {
        if (!HeadlessE2ETestSession.IsEnvironmentReady(out _))
        {
            return;
        }

        if (!ControllableClientSession.IsVisualEnvironmentReady(out _))
        {
            return;
        }

        using var session = new ControllableClientSession(
            TestLaunchOptions.VisualWithDebug(),
            nameof(VisualLaunchStartsWithDebugUiEnabled));

        bool reachedMainMenu = session.WaitUntil(
            s => s.CurrentScreen is MainMenuScreen,
            TimeSpan.FromSeconds(30));

        Assert.True(reachedMainMenu, "Game did not reach the main menu in time.");
        Assert.True(session.IsDebugUiEnabled, "Debug UI should be enabled at launch in visual debug mode.");
    }
}
