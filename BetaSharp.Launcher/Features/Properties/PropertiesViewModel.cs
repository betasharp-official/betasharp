using System.ComponentModel.DataAnnotations;
using BetaSharp.Launcher.Features.Hosting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetaSharp.Launcher.Features.Properties;

// Props should not be hard-coded.
internal sealed partial class PropertiesViewModel(NavigationService navigationService) : ObservableValidator
{
    [ObservableProperty]
    [RegularExpression(@"^$|^(\d{1,3}\.){3}\d{1,3}$", ErrorMessage = "Server IP must be empty or a valid IPv4 address.")]
    public partial string? ServerIp { get; set; }

    [ObservableProperty]
    public partial bool DualStack { get; set; } = false;

    [ObservableProperty]
    [Range(1, 65535, ErrorMessage = "Server port must be between 1 and 65535.")]
    public partial int ServerPort { get; set; } = 25565;

    [ObservableProperty]
    public partial bool OnlineMode { get; set; } = true;

    [ObservableProperty]
    public partial bool SpawnAnimals { get; set; } = true;

    [ObservableProperty]
    public partial bool Pvp { get; set; } = true;

    [ObservableProperty]
    public partial bool AllowFlight { get; set; } = false;

    [ObservableProperty]
    [Range(2, 32, ErrorMessage = "View distance must be between 2 and 32.")]
    public partial int ViewDistance { get; set; } = 10;

    [ObservableProperty]
    [Range(1, 500, ErrorMessage = "Max players must be between 1 and 500.")]
    public partial int MaxPlayers { get; set; } = 20;

    [ObservableProperty]
    public partial bool WhiteList { get; set; } = false;

    [ObservableProperty]
    [Required(ErrorMessage = "Level name is required.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Level name cannot be empty.")]
    public partial string LevelName { get; set; } = "world";

    [ObservableProperty]
    public partial string? LevelSeed { get; set; }

    [ObservableProperty]
    public partial int LevelType { get; set; }

    [ObservableProperty]
    public partial string? GeneratorSettings { get; set; }

    [ObservableProperty]
    public partial bool SpawnMonsters { get; set; } = true;

    [ObservableProperty]
    [Range(0, 9999, ErrorMessage = "Spawn region size must be between 0 and 9999.")]
    public partial int SpawnRegionSize { get; set; } = 196;

    [ObservableProperty]
    public partial bool AllowNether { get; set; } = true;

    [RelayCommand]
    private void Save()
    {
#pragma warning disable IL2026
        ValidateAllProperties();
#pragma warning restore IL2026

        if (HasErrors)
        {
            return;
        }

        navigationService.Navigate<HostingViewModel>();
    }

    [RelayCommand]
    private void Back()
    {
        navigationService.Navigate<HostingViewModel>();
    }
}
