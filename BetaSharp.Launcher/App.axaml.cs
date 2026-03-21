using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BetaSharp.Launcher.Features;
using BetaSharp.Launcher.Features.Shell;
using BetaSharp.Launcher.Features.Splash;
using Microsoft.Extensions.DependencyInjection;

namespace BetaSharp.Launcher;

internal sealed class App : Application
{
    public static string Folder { get; }

    private readonly IServiceProvider _services = Bootstrapper.Build();

    // Taken from BetaSharp.Client, should a shared project be created?
    static App()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrEmpty(home))
        {
            home = ".";
        }

        string path;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "." + nameof(BetaSharp));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            path = Path.Combine(home, "Library", "Application Support", nameof(BetaSharp));
        }
        else
        {
            string? xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            path = !string.IsNullOrEmpty(xdg) ? Path.Combine(xdg, nameof(BetaSharp)) : Path.Combine(home, ".local", "share", nameof(BetaSharp));
        }

        Folder = Path.Combine(path, "launcher");

        Directory.CreateDirectory(Folder);
    }

    public override void Initialize()
    {
        DataTemplates.Add(_services.GetRequiredService<ViewLocator>());
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _services
                .GetRequiredService<NavigationService>()
                .Navigate<SplashViewModel>();

            desktop.MainWindow = _services.GetRequiredService<ShellView>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
