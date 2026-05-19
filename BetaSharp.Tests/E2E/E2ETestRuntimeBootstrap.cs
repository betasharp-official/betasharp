using BetaSharp;

namespace BetaSharp.Tests.E2E;

internal static class E2ETestRuntimeBootstrap
{
    private static readonly Lock s_lock = new();
    private static bool s_initialized;

    public static void EnsureInitialized()
    {
        lock (s_lock)
        {
            if (s_initialized)
            {
                return;
            }

            string logRoot = Path.Combine(Path.GetTempPath(), "BetaSharp-E2E-Logs");
            Directory.CreateDirectory(logRoot);

            Log.Instance.Initialize(logRoot);
            AssetManager.Initialize(AssetManager.AssetProfile.Full);
            Bootstrap.Initialize();

            s_initialized = true;
        }
    }
}
