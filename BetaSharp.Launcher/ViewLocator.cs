using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using BetaSharp.Launcher.Features.Authentication;
using BetaSharp.Launcher.Features.Home;
using BetaSharp.Launcher.Features.Hosting;
using BetaSharp.Launcher.Features.Properties;
using BetaSharp.Launcher.Features.Splash;
using Microsoft.Extensions.DependencyInjection;

namespace BetaSharp.Launcher;

internal sealed class ViewLocator(IServiceProvider services) : IDataTemplate
{
    private readonly FrozenDictionary<string, Func<Control>> _viewFactories = new Dictionary<string, Func<Control>>
    {
        { nameof(SplashViewModel), services.GetRequiredService<SplashView> },
        { nameof(AuthenticationViewModel), services.GetRequiredService<AuthenticationView> },
        { nameof(HomeViewModel), services.GetRequiredService<HomeView> },
        { nameof(HostingViewModel), services.GetRequiredService<HostingView> },
        { nameof(PropertiesViewModel), services.GetRequiredService<PropertiesView> }
    }.ToFrozenDictionary();

    public Control Build(object? instance)
    {
        string? name = instance?.GetType().Name;

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return !_viewFactories.TryGetValue(name, out var factory)
            ? throw new ArgumentOutOfRangeException(nameof(instance))
            : factory();
    }

    public bool Match(object? instance)
    {
        return instance is INotifyPropertyChanged;
    }
}
