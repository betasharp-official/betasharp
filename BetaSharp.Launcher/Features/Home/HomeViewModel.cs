using System;
using System.Threading.Tasks;
using BetaSharp.Launcher.Features.Authentication;
using BetaSharp.Launcher.Features.Hosting;
using BetaSharp.Launcher.Features.Sessions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BetaSharp.Launcher.Features.Home;

internal sealed partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    public partial Session? Session { get; set; }

    private readonly NavigationService _navigationService;
    private readonly StorageService _storageService;
    private readonly ProcessService _processService;

    public HomeViewModel(NavigationService navigationService, StorageService storageService, ProcessService processService)
    {
        _navigationService = navigationService;
        _storageService = storageService;
        _processService = processService;

        WeakReferenceMessenger.Default.Register<HomeViewModel, SessionMessage>(
            this,
            static (viewModel, message) => viewModel.Session = message.Session);
    }

    [RelayCommand]
    private void SignOut()
    {
        _navigationService.Navigate<AuthenticationViewModel>();
        _storageService.Delete(nameof(Session));
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (Session?.HasExpired ?? true)
        {
            _navigationService.Navigate<AuthenticationViewModel>();
            return;
        }

        using var process = await _processService.StartAsync(Kind.Client, Session.Name, Session.Token);
        await process.WaitForExitAsync();
    }

    [RelayCommand]
    private void Host()
    {
        _navigationService.Navigate<HostingViewModel>();
    }
}
