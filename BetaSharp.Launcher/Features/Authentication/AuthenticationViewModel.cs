using System.Threading.Tasks;
using BetaSharp.Launcher.Features.Alert;
using BetaSharp.Launcher.Features.Home;
using BetaSharp.Launcher.Features.Sessions;
using BetaSharp.Launcher.Features.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BetaSharp.Launcher.Features.Authentication;

internal sealed partial class AuthenticationViewModel(
    AuthenticationService authenticationService,
    SessionService sessionService,
    AlertService alertService,
    NavigationService navigationService,
    StorageService storageService) : ObservableObject
{
    [RelayCommand]
    private async Task AuthenticateAsync()
    {
        string token = await authenticationService.AuthenticateAsync();

        var session = await sessionService.TryCreateAsync(token);

        if (session is null)
        {
            await alertService.ShowAsync("Authentication Failure", "The selected Microsoft account does not own a copy of Minecraft Java edition");
            return;
        }

        navigationService.Navigate<HomeViewModel>();
        WeakReferenceMessenger.Default.Send(new SessionMessage(session));

        await storageService.SetAsync(session, SessionSerializerContext.Default.Session);
    }
}
