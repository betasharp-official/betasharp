using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BetaSharp.Launcher.Features;
using BetaSharp.Launcher.Features.Hosting;
using BetaSharp.Launcher.Features.Shell;
using BetaSharp.Launcher.Features.Splash;
using Microsoft.Extensions.DependencyInjection;

namespace BetaSharp.Launcher;

internal sealed class App : Application
{
    public static string Folder { get; }

    private readonly IServiceProvider _services = Bootstrapper.Build();

    static App()
    {
        Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $".{nameof(BetaSharp)}", "launcher");
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
            desktop.Exit += DesktopOnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DesktopOnExit(object? sender, ControlledApplicationLifetimeExitEventArgs eventArgs)
    {
        _services
            .GetRequiredService<HostingViewModel>()
            .Stop();
    }
}
