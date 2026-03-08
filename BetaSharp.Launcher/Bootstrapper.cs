using System;
using System.IO;
using BetaSharp.Launcher.Features.Alert;
using BetaSharp.Launcher.Features.Authentication;
using BetaSharp.Launcher.Features.Home;
using BetaSharp.Launcher.Features.Mojang;
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

    [Singleton(typeof(ViewLocator))]
    [Singleton(typeof(AuthenticationService))]
    [Singleton(typeof(NavigationService))]
    [Singleton(typeof(AlertService))]
    [Singleton(typeof(ShellViewModel))]
    [Transient(typeof(SessionService))]
    [Transient(typeof(StorageService))]
    [Transient(typeof(ClientService))]
    [Transient(typeof(MojangClient))]
    [Transient(typeof(XboxClient))]
    [Transient(typeof(AuthenticationView))]
    [Transient(typeof(AuthenticationViewModel))]
    [Transient(typeof(HomeView))]
    [Transient(typeof(HomeViewModel))]
    [Transient(typeof(ShellView))]
    [Transient(typeof(SplashView))]
    [Transient(typeof(SplashViewModel))]
    private static partial void ConfigureServices(IServiceCollection services);
}
