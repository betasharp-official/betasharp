using System;
using System.IO;
using BetaSharp.Launcher.Features;
using BetaSharp.Launcher.Features.Alert;
using BetaSharp.Launcher.Features.Authentication;
using BetaSharp.Launcher.Features.Home;
using BetaSharp.Launcher.Features.Home.GitHub;
using BetaSharp.Launcher.Features.Hosting;
using BetaSharp.Launcher.Features.Mojang;
using BetaSharp.Launcher.Features.Properties;
using BetaSharp.Launcher.Features.Sessions;
using BetaSharp.Launcher.Features.Shell;
using BetaSharp.Launcher.Features.Splash;
using BetaSharp.Launcher.Features.Xbox;
using CommunityToolkit.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BetaSharp.Launcher;

internal static partial class Bootstrapper
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddHttpClient();

        services.AddLogging(builder =>
        {
            // Find a way to display class names and hide HttpClient's logs.
            const string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level} {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    Path.Combine(App.Folder, "logs", ".txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 5,
                    outputTemplate: template)
                .CreateLogger();

            builder.AddSerilog(Log.Logger);
        });

        ConfigureServices(services);

        return services.BuildServiceProvider();
    }

    // Services
    [Singleton(typeof(AuthenticationService))]
    [Singleton(typeof(NavigationService))]
    [Singleton(typeof(AlertService))]
    [Transient(typeof(SessionService))]
    [Transient(typeof(StorageService))]
    [Transient(typeof(MinecraftService))]
    [Transient(typeof(ProcessService))]

    // VMs
    [Singleton(typeof(ShellViewModel))]
    [Singleton(typeof(HostingViewModel))]
    [Singleton(typeof(HomeViewModel))]
    [Singleton(typeof(PropertiesViewModel))]
    [Transient(typeof(AuthenticationViewModel))]
    [Transient(typeof(SplashViewModel))]

    // Views
    [Singleton(typeof(HomeView))]
    [Transient(typeof(PropertiesView))]
    [Transient(typeof(ShellView))]
    [Transient(typeof(HostingView))]
    [Transient(typeof(AuthenticationView))]
    [Transient(typeof(SplashView))]

    // ...
    [Singleton(typeof(ViewLocator))]
    [Transient(typeof(MojangClient))]
    [Transient(typeof(XboxClient))]
    [Transient(typeof(GitHubClient))]
    private static partial void ConfigureServices(IServiceCollection services);
}
