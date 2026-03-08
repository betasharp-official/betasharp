using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace BetaSharp.Launcher.Features.Alert;

internal sealed class AlertService
{
    public async Task ShowAsync(string title, string message)
    {
        if (Application.Current?.ApplicationLifetime is not ClassicDesktopStyleApplicationLifetime lifetime)
        {
            return;
        }

        var window = lifetime.MainWindow;

        ArgumentNullException.ThrowIfNull(window);

        var view = new AlertView { Title = title, AlertBlock = { Text = message } };

        await view.ShowDialog(window);
    }
}
