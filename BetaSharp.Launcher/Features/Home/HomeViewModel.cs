using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using BetaSharp.Launcher.Features.Authentication;
using BetaSharp.Launcher.Features.Sessions;
using BetaSharp.Launcher.Features.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Launcher.Features.Home;

internal sealed partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    public partial Session? Session { get; set; }

    [ObservableProperty]
    public partial CroppedBitmap? Face { get; set; }

    private readonly ILogger<HomeViewModel> _logger;
    private readonly NavigationService _navigationService;
    private readonly StorageService _storageService;
    private readonly ClientService _clientService;
    private readonly SkinService _skinService;

    public HomeViewModel(
        ILogger<HomeViewModel> logger,
        NavigationService navigationService,
        StorageService storageService,
        ClientService clientService,
        SkinService skinService)
    {
        _logger = logger;
        _navigationService = navigationService;
        _storageService = storageService;
        _clientService = clientService;
        _skinService = skinService;

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

        await _clientService.DownloadAsync();

        var info = new ProcessStartInfo
        {
            Arguments = $"{Session.Name} {Session.Token} {Session.Skin}",
            CreateNoWindow = true,
            FileName = Path.Combine(AppContext.BaseDirectory, "Client", "BetaSharp.Client")
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

    // Find a better way to do this.
    protected override async void OnPropertyChanged(PropertyChangedEventArgs eventArgs)
    {
        try
        {
            base.OnPropertyChanged(eventArgs);

            if (eventArgs.PropertyName is not nameof(Session))
            {
                return;
            }

            const string steve = "http://textures.minecraft.net/texture/31f477eb1a7beee631c2ca64d06f8f68fa93a3386d04452ab27f43acdf1b60cb";
            Face = await _skinService.GetFaceAsync(Session?.Skin ?? steve);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occured while updating the face");
        }
    }
}
