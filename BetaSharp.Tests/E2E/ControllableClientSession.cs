using System.Diagnostics;
using BetaSharp.Client.UI;
using BetaSharp.Client.UI.Screens;
using BetaSharp.Client.UI.Screens.Menu;

namespace BetaSharp.Tests.E2E;

public sealed class ControllableClientSession : IDisposable
{
    private readonly Thread? _thread;
    private readonly TestLaunchOptions _options;
    private readonly HeadlessE2ETestSession? _headlessSession;
    private Exception? _runException;
    private bool _disposed;

    public BetaSharp.Client.BetaSharp? Game { get; }
    public UIScreen? CurrentScreen => Game?.CurrentScreen;
    public bool IsDebugUiEnabled => Game?.Options?.ShowDebugInfo ?? false;
    public bool IsMainMenuVisible => CurrentScreen is MainMenuScreen;

    public static bool IsVisualEnvironmentReady(out string reason)
    {
        if (OperatingSystem.IsLinux())
        {
            string? display = Environment.GetEnvironmentVariable("DISPLAY");
            string? wayland = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
            if (string.IsNullOrWhiteSpace(display) && string.IsNullOrWhiteSpace(wayland))
            {
                reason = "No graphical display detected (DISPLAY/WAYLAND_DISPLAY unset).";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    public ControllableClientSession(TestLaunchOptions options, string? testName = null)
    {
        _options = options;

        if (!HeadlessE2ETestSession.IsEnvironmentReady(out string reason))
        {
            throw new InvalidOperationException(reason);
        }

        E2ETestRuntimeBootstrap.EnsureInitialized();

        if (options.Mode == TestLaunchMode.Headless)
        {
            _headlessSession = new HeadlessE2ETestSession(testName);
            return;
        }

        if (!IsVisualEnvironmentReady(out string visualReason))
        {
            throw new InvalidOperationException(visualReason);
        }

        string playerName = $"e2e-{Guid.NewGuid():N}"[..12];
        Game = BetaSharp.Client.BetaSharp.CreateConfiguredInstance(
            playerName,
            "-",
            new BetaSharp.Client.BetaSharpRuntimeOptions
            {
                ForceDebugUi = options.ForceDebugUi,
                SuppressProcessExit = true
            });

        _thread = new Thread(() =>
        {
            try
            {
                Game.Run();
            }
            catch (Exception ex)
            {
                _runException = ex;
            }
        })
        {
            IsBackground = true,
            Name = $"E2E Client Session ({testName ?? "visual"})"
        };

        _thread.Start();
    }

    public bool WaitUntil(Func<ControllableClientSession, bool> condition, TimeSpan timeout)
    {
        Stopwatch sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            ThrowIfFailed();

            if (condition(this))
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _headlessSession?.Dispose();

        if (Game is not null)
        {
            try
            {
                Game.Shutdown();
            }
            catch
            {
                // best effort shutdown only
            }
        }

        if (_thread is not null && _thread.IsAlive)
        {
            _thread.Join(TimeSpan.FromSeconds(10));
        }

        ThrowIfFailed();
    }

    private void ThrowIfFailed()
    {
        if (_runException is not null)
        {
            throw new InvalidOperationException("Controllable client session crashed.", _runException);
        }
    }
}
