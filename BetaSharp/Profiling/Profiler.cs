using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BetaSharp.Profiling;

/// <summary>
/// Lightweight hierarchical profiler supporting two explicit threads: Main and Server.
///
/// Scope naming convention: use PascalCase for all scope names. Do not use dots, underscores,
/// spaces, or any other separators within a scope name. Express hierarchy purely through nesting
/// of <see cref="Begin"/> calls — each level adds a "/" separator to the stored path.
///
/// Correct:   using (Profiler.Begin("Render")) { using (Profiler.Begin("Entities")) { ... } }
/// Incorrect: Profiler.Begin("render.entities") or Profiler.Begin("Render/Entities")
/// </summary>
public static class Profiler
{
    public const int HistoryLength = 300;

    private sealed class ScopeData(string name)
    {
        public readonly string Name = name;
        public double Last;
        public double Avg;
        public double CurrentPeriodMax;
        public double PreviousPeriodMax;
        public readonly double[] History = new double[HistoryLength];
        public int HistoryHead;

        public void Update(double ms)
        {
            Last = ms;
            Avg = Avg == 0 ? ms : (Avg * 0.95) + (ms * 0.05);
            if (ms > CurrentPeriodMax) CurrentPeriodMax = ms;
        }
    }

    internal sealed class ThreadContext(string displayName)
    {
        public readonly string DisplayName = displayName;

        private readonly Dictionary<string, ScopeData> _scopes = [];
        private readonly Stack<string> _pathStack = new();
        private double _periodTimer;
        private SnapshotEntry[]? _snapshot;

        public string GetCurrentPath(string name)
            => _pathStack.Count == 0 ? name : _pathStack.Peek() + "/" + name;

        internal ProfilerScope BeginScope(string name)
        {
            string path = GetCurrentPath(name);
            _pathStack.Push(path);
            return new ProfilerScope(path, this);
        }

        internal void EndScope(string path, double ms)
        {
            _pathStack.TryPop(out _);
            if (!_scopes.TryGetValue(path, out ScopeData? data))
            {
                data = new ScopeData(path);
                _scopes[path] = data;
            }
            data.Update(ms);
        }

        internal void RecordDirect(string path, double ms)
        {
            if (!_scopes.TryGetValue(path, out ScopeData? data))
            {
                data = new ScopeData(path);
                _scopes[path] = data;
            }
            data.Update(ms);
        }

        public void RollPeriodMax(double dt)
        {
            _periodTimer += dt;
            if (_periodTimer < 1.0) return;
            _periodTimer = 0;
            foreach (ScopeData scope in _scopes.Values)
            {
                scope.PreviousPeriodMax = scope.CurrentPeriodMax;
                scope.CurrentPeriodMax = 0;
            }
        }

        public void CaptureFrame()
        {
            foreach (ScopeData scope in _scopes.Values)
            {
                scope.History[scope.HistoryHead] = scope.Last;
                scope.HistoryHead = (scope.HistoryHead + 1) % HistoryLength;
            }

            var snap = new SnapshotEntry[_scopes.Count];
            int i = 0;
            foreach (ScopeData data in _scopes.Values)
            {
                snap[i++] = new SnapshotEntry(
                    data.Name,
                    data.Last,
                    data.Avg,
                    Math.Max(data.CurrentPeriodMax, data.PreviousPeriodMax),
                    data.History,
                    data.HistoryHead);
            }
            Volatile.Write(ref _snapshot, snap);
        }

        public SnapshotEntry[]? GetSnapshot() => Volatile.Read(ref _snapshot);
    }

    /// <summary>
    /// A point-in-time snapshot of a single profiler scope.
    /// </summary>
    public readonly record struct SnapshotEntry(
        string Name,
        double Last,
        double Avg,
        double Max,
        double[] History,
        int HistoryHead);

    /// <summary>
    /// A zero-allocation profiling scope. Dispose via <c>using</c> to stop the timer.
    /// </summary>
    public readonly ref struct ProfilerScope
    {
        private readonly long _startTicks;
        private readonly string? _path;
        private readonly ThreadContext? _context;

        internal ProfilerScope(string path, ThreadContext context)
        {
            _startTicks = Stopwatch.GetTimestamp();
            _path = path;
            _context = context;
        }

        public readonly void Dispose()
        {
            if (_context == null) return;
            double ms = (Stopwatch.GetTimestamp() - _startTicks) * 1000.0 / Stopwatch.Frequency;
            _context.EndScope(_path!, ms);
        }
    }

    private static ThreadContext? s_mainContext;
    private static ThreadContext? s_serverContext;

    [ThreadStatic]
    private static ThreadContext? s_currentContext;

    /// <summary>
    /// Call once from the main client thread before the game loop starts.
    /// </summary>
    public static void RegisterMainThread()
    {
        s_mainContext = new ThreadContext("Main");
        s_currentContext = s_mainContext;
    }

    /// <summary>
    /// Call once from the server thread at the start of its run method.
    /// </summary>
    public static void RegisterServerThread()
    {
        s_serverContext = new ThreadContext("Server");
        s_currentContext = s_serverContext;
    }

    /// <summary>
    /// Begins a profiling scope. Must be disposed via <c>using</c> to record the measurement.
    /// Returns a no-op default when the profiler is disabled or called from an unregistered thread.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ProfilerScope Begin(string name)
    {
        if (s_currentContext == null)
            return default;
        return s_currentContext.BeginScope(name);
    }

    /// <summary>
    /// Records a pre-measured value (in milliseconds) under the given name at the current scope level.
    /// </summary>
    public static void Record(string name, double milliseconds)
    {
        if (s_currentContext == null) return;
        string path = s_currentContext.GetCurrentPath(name);
        s_currentContext.RecordDirect(path, milliseconds);
    }

    /// <summary>
    /// Advances the one-second period-max timer. Call once per frame from the main thread.
    /// </summary>
    public static void Update(double dt)
    {
        s_mainContext?.RollPeriodMax(dt);
        s_serverContext?.RollPeriodMax(dt);
    }

    /// <summary>
    /// Call once per frame from the main thread.
    /// </summary>
    public static void CaptureFrame()
    {
        s_mainContext?.CaptureFrame();
        s_serverContext?.CaptureFrame();
    }

    /// <summary>
    /// Returns the latest snapshot entries from all registered threads, sorted by name.
    /// </summary>
    public static IEnumerable<(string Name, double Last, double Avg, double Max, double[] History, int HistoryHead)> GetStats()
    {
        var result = new List<(string, double, double, double, double[], int)>();
        AppendContext(s_mainContext, "Main", result);
        AppendContext(s_serverContext, "Server", result);
        return result.OrderBy(x => x.Item1);
    }

    private static void AppendContext(
        ThreadContext? ctx,
        string prefix,
        List<(string, double, double, double, double[], int)> result)
    {
        if (ctx?.GetSnapshot() is not { } snap) return;
        foreach (SnapshotEntry e in snap)
            result.Add(($"[{prefix}] {e.Name}", e.Last, e.Avg, e.Max, e.History, e.HistoryHead));
    }
}
