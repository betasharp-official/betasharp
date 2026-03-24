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
    [ObservableProperty]
    public partial string? Link { get; set; }

    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial bool IsReady { get; set; }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await authenticationService.InitializeAsync();
    }

    [RelayCommand]
    private async Task UseWebAsync()
    {
        string token = await authenticationService.AuthenticateWebAsync();
        await AuthenticateAsync(token);
    }

    [RelayCommand]
    private async Task UseCodeAsync()
    {
        string token = await authenticationService.AuthenticateCodeAsync(callback =>
        {
            Link = callback.VerificationUrl;
            Message = $"Use a Web browser to open {callback.VerificationUrl} and enter the code {callback.UserCode} to authenticate";

            // Need a way to detect timeouts.
            IsReady = true;

            return Task.CompletedTask;
        });

        await AuthenticateAsync(token);

        IsReady = false;
    }

    private async Task AuthenticateAsync(string token)
    {
        try
        {
            var session = await sessionService.TryCreateAsync(token);

            if (string.IsNullOrWhiteSpace(session?.Token))
            {
                await alertService.ShowAsync(
                    "License Required",
                    "The selected Microsoft account does not own a copy of Minecraft Java edition");

                return;
            }

            navigationService.Navigate<HomeViewModel>();

            WeakReferenceMessenger.Default.Send(new SessionMessage(session));

            await storageService.SetAsync(session, SessionSerializerContext.Default.Session);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An exception occurred while trying to authenticate");
            storageService.Delete(nameof(Session));

            await alertService.ShowAsync(
                "Uh-oh!",
                "Try again shortly. If the problem persists, create an issue on GitHub."
                + Environment.NewLine
                + "https://github.com/Fazin85/betasharp/issues");
        }
    }
}
