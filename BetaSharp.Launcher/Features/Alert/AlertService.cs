using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace BetaSharp.Launcher.Features.Alert;

internal sealed class AlertService
{
    private bool _isOpen;

    public async Task ShowAsync(string title, string message)
    {
        if (Application.Current?.ApplicationLifetime is not ClassicDesktopStyleApplicationLifetime lifetime)
        {
            return;
        }

        if (_isOpen)
        {
            throw new InvalidOperationException("Alert window already open.");
        }

        _isOpen = true;

        var window = lifetime.MainWindow;

        ArgumentNullException.ThrowIfNull(window);

        var view = new AlertView
        {
            Title = title,
            AlertBlock = { Text = message }
        };

        try
        {
            await view.ShowDialog(window);
        }
        catch
        {
            _isOpen = false;
        }
    }
}
