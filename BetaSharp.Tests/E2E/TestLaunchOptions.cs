namespace BetaSharp.Tests.E2E;

public enum TestLaunchMode
{
    Headless,
    Visual
}

public sealed record TestLaunchOptions
{
    public required TestLaunchMode Mode { get; init; }
    public bool ForceDebugUi { get; init; }
    public bool AutoStartWorld { get; init; }
    public string? WorldName { get; init; }

    public static TestLaunchOptions Headless() => new()
    {
        Mode = TestLaunchMode.Headless
    };

    public static TestLaunchOptions VisualWithDebug() => new()
    {
        Mode = TestLaunchMode.Visual,
        ForceDebugUi = true
    };
}
