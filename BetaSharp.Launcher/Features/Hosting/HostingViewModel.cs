using System.Threading.Tasks;
using BetaSharp.Launcher.Features.Home;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetaSharp.Launcher.Features.Hosting;

internal sealed partial class HostingViewModel(NavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    public partial string Message { get; set; } = "Run";

    private bool _isRunning;

    [RelayCommand]
    private async Task RunAsync()
    {
        if (_isRunning)
        {
            Message = "Stopping";

            await Task.Delay(1000);

            Message = "Run";

            _isRunning = false;

            return;
        }

        _isRunning = true;

        Message = "Initializing";

        await Task.Delay(1000);

        Message = "Stop";
    }

    [RelayCommand]
    private void Back()
    {
        navigationService.Navigate<HomeViewModel>();
    }
}
