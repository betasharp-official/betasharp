using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace BetaSharp.Launcher.Features.Home;

internal sealed class DedicatedServerService(ClientService clientService) : IDisposable
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    private Process? _process;
    private bool _disposed;

    private DedicatedServerState _state = DedicatedServerState.Stopped;
    private string _status = "Dedicated server is stopped.";

    public event EventHandler? StatusChanged;

    public DedicatedServerState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
    }

    public string Status
    {
        get
        {
            lock (_stateLock)
            {
                return _status;
            }
        }
    }

    public async Task StartAsync()
    {
        await _gate.WaitAsync();
        try
        {
            ThrowIfDisposed();

            DedicatedServerState state = State;
            if (state is DedicatedServerState.Starting or DedicatedServerState.Running or DedicatedServerState.Stopping)
            {
                return;
            }

            SetState(DedicatedServerState.Starting, "Starting dedicated server...");

            string directory = Path.Combine(AppContext.BaseDirectory, "Server");
            Directory.CreateDirectory(directory);

            try
            {
                await clientService.DownloadAsync(directory);
            }
            catch (Exception exception)
            {
                SetState(DedicatedServerState.Failed, $"Failed to prepare b1.7.3.jar: {exception.Message}");
                return;
            }

            string? executablePath = ResolveExecutablePath(directory);
            if (string.IsNullOrEmpty(executablePath))
            {
                SetState(DedicatedServerState.Failed, "Dedicated server executable was not found.");
                return;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    FileName = executablePath,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = directory
                },
                EnableRaisingEvents = true
            };

            process.Exited += OnProcessExited;
            process.OutputDataReceived += OnProcessOutput;
            process.ErrorDataReceived += OnProcessOutput;

            try
            {
                if (!process.Start())
                {
                    SetState(DedicatedServerState.Failed, "Failed to launch dedicated server process.");
                    process.Dispose();
                    return;
                }
            }
            catch (Exception exception)
            {
                SetState(DedicatedServerState.Failed, $"Failed to launch dedicated server: {exception.Message}");
                process.Dispose();
                return;
            }

            lock (_stateLock)
            {
                _process = process;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            SetState(DedicatedServerState.Running, "Dedicated server is running.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync();
        try
        {
            ThrowIfDisposed();

            Process? process;
            lock (_stateLock)
            {
                process = _process;
            }

            if (process is null)
            {
                if (State != DedicatedServerState.Failed)
                {
                    SetState(DedicatedServerState.Stopped, "Dedicated server is stopped.");
                }

                return;
            }

            SetState(DedicatedServerState.Stopping, "Stopping dedicated server...");

            await StopProcessAsync(process);

            lock (_stateLock)
            {
                if (ReferenceEquals(_process, process))
                {
                    _process = null;
                }
            }

            process.Dispose();

            if (State != DedicatedServerState.Failed)
            {
                SetState(DedicatedServerState.Stopped, "Dedicated server is stopped.");
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Wait();
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            Process? process;
            lock (_stateLock)
            {
                process = _process;
                _process = null;
            }

            if (process is not null)
            {
                SetState(DedicatedServerState.Stopping, "Stopping dedicated server...");

                try
                {
                    if (!process.HasExited)
                    {
                        TryWriteStop(process);

                        if (!process.WaitForExit((int)StopTimeout.TotalMilliseconds))
                        {
                            try
                            {
                                process.Kill(entireProcessTree: true);
                            }
                            catch (Exception)
                            {
                                // Ignore shutdown failures.
                            }

                            process.WaitForExit();
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore shutdown failures.
                }
                finally
                {
                    process.Dispose();
                }
            }

            SetState(DedicatedServerState.Stopped, "Dedicated server is stopped.");
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DedicatedServerService));
        }
    }

    private static string? ResolveExecutablePath(string directory)
    {
        string primaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "BetaSharp.Server.exe" : "BetaSharp.Server";
        string primaryPath = Path.Combine(directory, primaryName);
        if (File.Exists(primaryPath))
        {
            return primaryPath;
        }

        string fallbackName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "BetaSharp.Server" : "BetaSharp.Server.exe";
        string fallbackPath = Path.Combine(directory, fallbackName);
        if (File.Exists(fallbackPath))
        {
            return fallbackPath;
        }

        return null;
    }

    private void OnProcessOutput(object? sender, DataReceivedEventArgs args)
    {
        string? line = args.Data;
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        if (line.Contains("FAILED TO BIND TO PORT!", StringComparison.OrdinalIgnoreCase))
        {
            _ = HandleFailureAsync("Dedicated server failed to bind to the configured port.");
        }
        else if (line.Contains("Failed to start the BetaSharp server", StringComparison.OrdinalIgnoreCase))
        {
            _ = HandleFailureAsync("Dedicated server failed during startup. Check server logs.");
        }
    }

    private void OnProcessExited(object? sender, EventArgs args)
    {
        if (sender is not Process process)
        {
            return;
        }

        bool isCurrent;
        DedicatedServerState state;

        lock (_stateLock)
        {
            isCurrent = ReferenceEquals(_process, process);
            if (isCurrent)
            {
                _process = null;
            }

            state = _state;
        }

        if (!isCurrent)
        {
            return;
        }

        if (state == DedicatedServerState.Failed)
        {
            return;
        }

        if (state == DedicatedServerState.Stopping)
        {
            SetState(DedicatedServerState.Stopped, "Dedicated server is stopped.");
            return;
        }

        SetState(DedicatedServerState.Stopped, "Dedicated server exited.");
    }

    private async Task HandleFailureAsync(string status)
    {
        bool gateAcquired = false;

        try
        {
            await _gate.WaitAsync();
            gateAcquired = true;

            if (_disposed || State == DedicatedServerState.Failed)
            {
                return;
            }

            Process? process;
            lock (_stateLock)
            {
                process = _process;
            }

            SetState(DedicatedServerState.Failed, status);

            if (process is null)
            {
                return;
            }

            await StopProcessAsync(process);

            lock (_stateLock)
            {
                if (ReferenceEquals(_process, process))
                {
                    _process = null;
                }
            }

            process.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // The service is being disposed while processing output.
        }
        finally
        {
            if (gateAcquired)
            {
                _gate.Release();
            }
        }
    }

    private static async Task StopProcessAsync(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            await process.StandardInput.WriteLineAsync("stop");
            await process.StandardInput.FlushAsync();
        }
        catch (Exception)
        {
            // The process may already be gone.
        }

        Task waitTask = process.WaitForExitAsync();
        Task timeoutTask = Task.Delay(StopTimeout);

        if (await Task.WhenAny(waitTask, timeoutTask) == waitTask)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception)
        {
            // Ignore kill failures.
        }

        try
        {
            await process.WaitForExitAsync();
        }
        catch (Exception)
        {
            // Ignore wait failures.
        }
    }

    private static void TryWriteStop(Process process)
    {
        try
        {
            process.StandardInput.WriteLine("stop");
            process.StandardInput.Flush();
        }
        catch (Exception)
        {
            // The process may already be gone.
        }
    }

    private void SetState(DedicatedServerState state, string status)
    {
        bool changed;
        lock (_stateLock)
        {
            changed = _state != state || !string.Equals(_status, status, StringComparison.Ordinal);
            _state = state;
            _status = status;
        }

        if (!changed)
        {
            return;
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);
    }
}
