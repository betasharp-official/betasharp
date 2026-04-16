using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BetaSharp.Client.Diagnostics;

internal readonly record struct DebugSystemSnapshot(
    string GpuName,
    string GpuVram,
    string OpenGlVersion,
    string GlslVersion,
    string DriverVersion,
    string CpuName,
    int CpuCoreCount,
    string OsDescription,
    string DotNetRuntime)
{
    public static DebugSystemSnapshot Empty { get; } = new(
        DebugTelemetry.UnknownValue,
        DebugTelemetry.UnknownValue,
        DebugTelemetry.UnknownValue,
        DebugTelemetry.UnknownValue,
        DebugTelemetry.UnknownValue,
        DebugTelemetry.UnknownValue,
        Environment.ProcessorCount,
        DebugTelemetry.UnknownValue,
        DebugTelemetry.UnknownValue);
}

internal readonly record struct DebugGraphicsApiSnapshot(
    string GpuName,
    string GpuVram,
    string ApiVersion,
    string ShaderLanguageVersion,
    string DriverVersion)
{
    public static DebugGraphicsApiSnapshot Empty { get; } = new(
        DebugTelemetry.UnknownValue,
        DebugTelemetry.UnknownValue,
        DebugTelemetry.UnknownValue,
        DebugTelemetry.UnknownValue,
        DebugTelemetry.UnknownValue);
}

internal readonly record struct DebugFrameStatsSnapshot(
    int SampleCount,
    double AverageFrameTimeMs,
    double MinFps,
    double MaxFps)
{
    public static DebugFrameStatsSnapshot Empty { get; } = new(0, 0.0D, 0.0D, 0.0D);
    public bool HasData => SampleCount > 0;
}

internal sealed class DebugTelemetry
{
    internal const string UnknownValue = "N/A";
    private const int FrameHistorySize = 600;

    private readonly double[] _frameTimesMs = new double[FrameHistorySize];
    private int _nextFrameIndex;
    private int _frameSampleCount;

    private DebugSystemSnapshot _systemSnapshot = DebugSystemSnapshot.Empty;

    public DebugSystemSnapshot SystemSnapshot => _systemSnapshot;

    public void CaptureSystemInfo(DebugGraphicsApiSnapshot graphicsApiSnapshot)
    {
        string apiVersion = SafeValue(graphicsApiSnapshot.ApiVersion);
        string driverVersion = SafeValue(graphicsApiSnapshot.DriverVersion);
        if (driverVersion == UnknownValue)
        {
            driverVersion = ParseDriverVersion(apiVersion);
        }

        _systemSnapshot = new DebugSystemSnapshot(
            GpuName: SafeValue(graphicsApiSnapshot.GpuName),
            GpuVram: SafeValue(graphicsApiSnapshot.GpuVram),
            OpenGlVersion: apiVersion,
            GlslVersion: SafeValue(graphicsApiSnapshot.ShaderLanguageVersion),
            DriverVersion: driverVersion,
            CpuName: GetCpuName(),
            CpuCoreCount: Environment.ProcessorCount,
            OsDescription: SafeValue(RuntimeInformation.OSDescription),
            DotNetRuntime: SafeValue(RuntimeInformation.FrameworkDescription));
    }

    public void RecordFrameTime(double frameTimeMs)
    {
        if (double.IsNaN(frameTimeMs) || double.IsInfinity(frameTimeMs) || frameTimeMs <= 0.0D)
        {
            return;
        }

        _frameTimesMs[_nextFrameIndex] = frameTimeMs;
        _nextFrameIndex = (_nextFrameIndex + 1) % _frameTimesMs.Length;
        if (_frameSampleCount < _frameTimesMs.Length)
        {
            _frameSampleCount++;
        }
    }

    public DebugFrameStatsSnapshot GetFrameStatsSnapshot()
    {
        if (_frameSampleCount == 0)
        {
            return DebugFrameStatsSnapshot.Empty;
        }

        double[] samples = new double[_frameSampleCount];
        int start = (_nextFrameIndex - _frameSampleCount + _frameTimesMs.Length) % _frameTimesMs.Length;
        for (int i = 0; i < _frameSampleCount; i++)
        {
            samples[i] = _frameTimesMs[(start + i) % _frameTimesMs.Length];
        }

        double totalMs = 0.0D;
        double minFrameTimeMs = double.MaxValue;
        double maxFrameTimeMs = double.MinValue;
        foreach (double sample in samples)
        {
            totalMs += sample;
            if (sample < minFrameTimeMs)
            {
                minFrameTimeMs = sample;
            }

            if (sample > maxFrameTimeMs)
            {
                maxFrameTimeMs = sample;
            }
        }

        double averageFrameMs = totalMs / _frameSampleCount;

        return new DebugFrameStatsSnapshot(
            SampleCount: _frameSampleCount,
            AverageFrameTimeMs: averageFrameMs,
            MinFps: ToFps(maxFrameTimeMs),
            MaxFps: ToFps(minFrameTimeMs));
    }

    private static double ToFps(double frameTimeMs)
    {
        if (frameTimeMs <= 0.0D || double.IsNaN(frameTimeMs) || double.IsInfinity(frameTimeMs))
        {
            return 0.0D;
        }

        return 1000.0D / frameTimeMs;
    }

    private static string ParseDriverVersion(string glVersion)
    {
        if (glVersion == UnknownValue)
        {
            return UnknownValue;
        }

        Match mesaMatch = Regex.Match(glVersion, @"Mesa\s+[\w\.\-]+", RegexOptions.IgnoreCase);
        if (mesaMatch.Success)
        {
            return mesaMatch.Value;
        }

        MatchCollection numericMatches = Regex.Matches(glVersion, @"\d+(?:\.\d+){1,3}");
        for (int i = numericMatches.Count - 1; i >= 0; i--)
        {
            string candidate = numericMatches[i].Value;
            if (!glVersion.StartsWith(candidate, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        int openParenIndex = glVersion.IndexOf('(');
        if (openParenIndex >= 0 && openParenIndex + 1 < glVersion.Length)
        {
            string inParens = glVersion[(openParenIndex + 1)..].TrimEnd(')').Trim();
            if (!string.IsNullOrWhiteSpace(inParens))
            {
                return inParens;
            }
        }

        return glVersion;
    }
    private static string GetCpuName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string? processorIdentifier = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
            if (!string.IsNullOrWhiteSpace(processorIdentifier))
            {
                return processorIdentifier.Trim();
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string? cpuName = TryReadValueFromFile("/proc/cpuinfo", "model name");
            if (!string.IsNullOrWhiteSpace(cpuName))
            {
                return cpuName;
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string? cpuName = TryRunCommand("sysctl", "-n machdep.cpu.brand_string");
            if (!string.IsNullOrWhiteSpace(cpuName))
            {
                return cpuName;
            }
        }

        return UnknownValue;
    }

    private static string? TryReadValueFromFile(string path, string key)
    {
        try
        {
            foreach (string line in File.ReadLines(path))
            {
                if (!line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int separator = line.IndexOf(':');
                return separator >= 0 ? line[(separator + 1)..].Trim() : line.Trim();
            }
        }
        catch
        {
        }

        return null;
    }

    private static string? TryRunCommand(string fileName, string arguments)
    {
        try
        {
            using Process process = new();
            process.StartInfo = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (!process.Start())
            {
                return null;
            }

            string stdout = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(1500))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                }

                return null;
            }

            if (process.ExitCode != 0)
            {
                return null;
            }

            string value = stdout.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }

    private static string SafeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? UnknownValue : value.Trim();
    }

}
