using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BetaSharp.Launcher.Features.Authentication;
using BetaSharp.Launcher.Features.Sessions;
using BetaSharp.Launcher.Features.Shell;
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
    private readonly ClientService _clientService;

    public HomeViewModel(NavigationService navigationService, StorageService storageService, ClientService clientService)
    {
        _navigationService = navigationService;
        _storageService = storageService;
        _clientService = clientService;

        WeakReferenceMessenger.Default.Register<HomeViewModel, SessionMessage>(
            this,
            static (viewModel, message) => viewModel.Session = message.Session);
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (Session?.HasExpired ?? true)
        {
            _navigationService.Navigate<AuthenticationViewModel>();
            return;
        }

        string directory = Path.Combine(AppContext.BaseDirectory, "Client");

        await _clientService.DownloadAsync(directory);

        var info = new ProcessStartInfo
        {
            Arguments = $"{Session.Name} {Session.Token}",
            CreateNoWindow = true,
            FileName = Path.Combine(directory, "BetaSharp.Client"),
            WorkingDirectory = directory
        };

        // Probably should move this into a service/view-model.
        using var process = Process.Start(info);

        ArgumentNullException.ThrowIfNull(process);

        await process.WaitForExitAsync();
    }

    [RelayCommand]
    private void SignOut()
    {
        _navigationService.Navigate<AuthenticationViewModel>();
        _storageService.Delete(nameof(Session));
    }
}
