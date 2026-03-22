using System;
using System.Threading.Tasks;
using BetaSharp.Launcher.Features.Alert;
using BetaSharp.Launcher.Features.Home;
using BetaSharp.Launcher.Features.Sessions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Launcher.Features.Authentication;

internal sealed partial class AuthenticationViewModel(
    ILogger<AuthenticationViewModel> logger,
    AuthenticationService authenticationService,
    SessionService sessionService,
    AlertService alertService,
    NavigationService navigationService,
    StorageService storageService) : ObservableObject
{
    [RelayCommand]
    private async Task AuthenticateAsync()
    {
        try
        {
            await authenticationService.InitializeAsync();

            string token = await authenticationService.AuthenticateAsync();

            var session = await sessionService.TryCreateAsync(token);

            if (string.IsNullOrWhiteSpace(session?.Token))
            {
                await alertService.ShowAsync(
                    "License Required",
                    "The selected Microsoft account does not own a copy of Minecraft Java edition");

                logger.LogWarning("Failed to verify ownership of a Minecraft Java edition copy");

                return;
            }

            navigationService.Navigate<HomeViewModel>();
            WeakReferenceMessenger.Default.Send(new SessionMessage(session));

            await storageService.SetAsync(session, SessionSerializerContext.Default.Session);

            logger.LogWarning("Successfully finished Minecraft authentication");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An exception occurred while trying to authenticate");
            storageService.Delete(nameof(Session));

            await alertService.ShowAsync(
                "Uh-oh!",
                "Try again shortly. If the issue persists, create a GitHub issue"
                + Environment.NewLine
                + "https://github.com/Fazin85/betasharp/issues");
        }
    }
}
