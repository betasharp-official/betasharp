using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using BetaSharp.Launcher.Features.Home;
using BetaSharp.Launcher.Features.Properties;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetaSharp.Launcher.Features.Hosting;

internal sealed partial class HostingViewModel(ProcessService processService, NavigationService navigationService) : ObservableObject
{
    public ObservableCollection<string> Logs { get; } = [];

    [ObservableProperty]
    public partial int Last { get; set; }

    [ObservableProperty]
    public partial string Input { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; set; } = "Run";

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    private Process? _process;

    [RelayCommand]
    private async Task RunAsync()
    {
        if (IsRunning)
        {
            ArgumentNullException.ThrowIfNull(_process);

            await WriteAsync("stop");

            await _process
                .WaitForExitAsync()
                .WaitAsync(TimeSpan.FromSeconds(5));

            Stop();

            return;
        }

        Logs.Clear();
        Last = -1;
        Input = string.Empty;
        Message = "Initializing";

        _process = await processService.StartAsync(Kind.Server);

        _process.EnableRaisingEvents = true;

        _process.BeginOutputReadLine();

        _process.Exited += OnExited;
        _process.OutputDataReceived += OnOutputDataReceived;

        Message = "Stop";

        IsRunning = true;
    }

    [RelayCommand]
    private async Task WriteAsync()
    {
        await WriteAsync(Input);
        Input = string.Empty;
    }

    [RelayCommand]
    private void Properties()
    {
        navigationService.Navigate<PropertiesViewModel>();
    }

    [RelayCommand]
    private void Back()
    {
        navigationService.Navigate<HomeViewModel>();
    }

    private void Stop()
    {
        Message = "Stopping";

        if (_process is not null)
        {
            try
            {
                _process.Exited -= OnExited;
                _process.OutputDataReceived -= OnOutputDataReceived;

                _process.CancelOutputRead();

                _process.Kill();
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }

        Message = "Run";

        IsRunning = false;
    }

    private async Task WriteAsync(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(_process);

        await _process.StandardInput.WriteLineAsync(input);
        await _process.StandardInput.FlushAsync();
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        if (string.IsNullOrWhiteSpace(eventArgs.Data))
        {
            return;
        }

        Logs.Add(eventArgs.Data);
        Last = Logs.Count - 1;
    }

    private void OnExited(object? sender, EventArgs eventArgs)
    {
        Stop();
    }
}
