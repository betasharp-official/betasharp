using System.Threading.Tasks;
using BetaSharp.Launcher.Features.Authentication;
using BetaSharp.Launcher.Features.Home;
using BetaSharp.Launcher.Features.Sessions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BetaSharp.Launcher.Features.Splash;

internal sealed partial class SplashViewModel(StorageService storageService, NavigationService navigationService) : ObservableObject
{
    [RelayCommand]
    private async Task InitializeAsync()
    {
        var session = await storageService.GetAsync(SessionSerializerContext.Default.Session);

        if (session?.HasExpired ?? true)
        {
            navigationService.Navigate<AuthenticationViewModel>();
        }
        else
        {
            navigationService.Navigate<HomeViewModel>();
            WeakReferenceMessenger.Default.Send(new SessionMessage(session));
        }
    }
}
