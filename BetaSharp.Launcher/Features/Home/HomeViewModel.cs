using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using BetaSharp.Launcher.Features.Accounts;
using BetaSharp.Launcher.Features.Authentication;
using BetaSharp.Launcher.Features.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetaSharp.Launcher.Features.Home;

internal sealed partial class HomeViewModel(
    NavigationService navigationService,
    AccountsService accountsService,
    ClientService clientService,
    SkinService skinService) : ObservableObject
{
    [ObservableProperty]
    public partial Account? Account { get; set; }

    [ObservableProperty]
    public partial CroppedBitmap? Face { get; set; }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        Account = await accountsService.GetAsync();

        ArgumentNullException.ThrowIfNull(Account);

        if (!string.IsNullOrWhiteSpace(Account.Skin))
        {
            Face = await skinService.GetFaceAsync(Account.Skin);
        }
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        // Check if account's token has expired.
        ArgumentNullException.ThrowIfNull(Account);

        await clientService.DownloadAsync();

        var info = new ProcessStartInfo
        {
            Arguments = $"{Account.Name} {Account.Token} {Account.Skin ?? string.Empty}",
            CreateNoWindow = true,
            FileName = Path.Combine(AppContext.BaseDirectory, "Client", "BetaSharp.Client")
        };

        // Probably should move this into a service/view-model.
        using var process = Process.Start(info);

        ArgumentNullException.ThrowIfNull(process);

        await process.WaitForExitAsync();
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        navigationService.Navigate<AuthenticationViewModel>();
        await accountsService.DeleteAsync();
        Face?.Dispose();
    }
}
