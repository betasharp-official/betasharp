using System;
using System.ComponentModel;
using BetaSharp.Launcher.Features.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace BetaSharp.Launcher.Features;

internal sealed class NavigationService(ShellViewModel shellViewModel, IServiceProvider services)
{
    public void Navigate<T>() where T : INotifyPropertyChanged
    {
        shellViewModel.Current = services.GetRequiredService<T>();
    }
}
