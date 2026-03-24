using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BetaSharp.Launcher.Features.Hosting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetaSharp.Launcher.Features.Properties;

// Props should not be hard-coded.
// Had to manually implement INotifyDataErrorInfo because ObservableValidator isn't AOT friendly.
internal sealed partial class PropertiesViewModel(NavigationService navigationService) : ObservableObject, INotifyDataErrorInfo
{
    [ObservableProperty]
    public partial string? ServerIp { get; set; }

    [ObservableProperty]
    public partial bool DualStack { get; set; } = false;

    [ObservableProperty]
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
    public partial int ViewDistance { get; set; } = 10;

    [ObservableProperty]
    public partial int MaxPlayers { get; set; } = 20;

    [ObservableProperty]
    public partial bool WhiteList { get; set; } = false;

    [ObservableProperty]
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
    public partial int SpawnRegionSize { get; set; } = 196;

    [ObservableProperty]
    public partial bool AllowNether { get; set; } = true;

    [ObservableProperty]
    public partial bool IsReady { get; set; }

    public bool HasErrors => _errors.Count != 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    private readonly Dictionary<string, string> _errors = [];
    private readonly string _path = Path.Combine(AppContext.BaseDirectory, nameof(Kind.Server), "server.properties");

    [RelayCommand]
    private async Task InitializeAsync()
    {
        IsReady = false;

        try
        {
            foreach (string line in await File.ReadAllLinesAsync(_path))
            {
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#') || trimmed.StartsWith('!'))
                {
                    continue;
                }

                int index = trimmed.IndexOf('=');

                if (index < 0)
                {
                    index = trimmed.IndexOf(':');
                }

                if (index < 0)
                {
                    continue;
                }

                string value = trimmed[(index + 1)..].Trim();

                switch (trimmed[..index].Trim())
                {
                    case "server-ip":
                        {
                            ServerIp = value;
                            break;
                        }

                    case "dual-stack" when bool.TryParse(value, out bool result):
                        {
                            DualStack = result;
                            break;
                        }

                    case "server-port" when int.TryParse(value, out int result):
                        {
                            ServerPort = result;
                            break;
                        }

                    case "online-mode" when bool.TryParse(value, out bool result):
                        {
                            OnlineMode = result;
                            break;
                        }

                    case "spawn-animals" when bool.TryParse(value, out bool result):
                        {
                            SpawnAnimals = result;
                            break;
                        }

                    case "pvp" when bool.TryParse(value, out bool result):
                        {
                            Pvp = result;
                            break;
                        }

                    case "allow-flight" when bool.TryParse(value, out bool result):
                        {
                            AllowFlight = result;
                            break;
                        }

                    case "view-distance" when int.TryParse(value, out int result):
                        {
                            ViewDistance = result;
                            break;
                        }

                    case "max-players" when int.TryParse(value, out int result):
                        {
                            MaxPlayers = result;
                            break;
                        }

                    case "white-list" when bool.TryParse(value, out bool result):
                        {
                            WhiteList = result;
                            break;
                        }

                    case "level-name" when !string.IsNullOrWhiteSpace(value):
                        {
                            LevelName = value;
                            break;
                        }

                    case "level-seed":
                        {
                            LevelSeed = value;
                            break;
                        }

                    case "level-type":
                        {
                            LevelType = value.ToUpperInvariant() switch
                            {
                                "DEFAULT" => 0,
                                "FLAT" => 1,
                                _ => 0
                            };
                            break;
                        }

                    case "generator-settings":
                        {
                            GeneratorSettings = value;
                            break;
                        }

                    case "spawn-monsters" when bool.TryParse(value, out bool result):
                        {
                            SpawnMonsters = result;
                            break;
                        }

                    case "spawn-region-size" when int.TryParse(value, out int result):
                        {
                            SpawnRegionSize = result;
                            break;
                        }

                    case "allow-nether" when bool.TryParse(value, out bool result):
                        {
                            AllowNether = result;
                            break;
                        }
                }
            }
        }
        catch (IOException)
        {
            await WriteAsync();
        }

        IsReady = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await WriteAsync();

        navigationService.Navigate<HostingViewModel>();
    }

    [RelayCommand]
    private void Back()
    {
        navigationService.Navigate<HostingViewModel>();
    }

    private async Task WriteAsync()
    {
        string levelType = LevelType switch
        {
            0 => "DEFAULT",
            1 => "FLAT",
            _ => "DEFAULT"
        };

        string value = $"""
                        # BetaSharp server properties
                        # Generated by BetaSharp's launcher
                        server-ip={ServerIp}
                        dual-stack={DualStack}
                        server-port={ServerPort}
                        online-mode={OnlineMode}
                        spawn-animals={SpawnAnimals}
                        pvp={Pvp}
                        allow-flight={AllowFlight}
                        view-distance={ViewDistance}
                        max-players={MaxPlayers}
                        white-list={WhiteList}
                        level-name={LevelName}
                        level-seed={LevelSeed}
                        level-type={levelType}
                        generator-settings={GeneratorSettings}
                        spawn-monsters={SpawnMonsters}
                        spawn-region-size={SpawnRegionSize}
                        allow-nether={AllowNether}
                        """;

        await File.WriteAllTextAsync(_path, value);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs eventArgs)
    {
        base.OnPropertyChanged(eventArgs);

        if (!string.IsNullOrWhiteSpace(ServerIp) && !IPAddress.TryParse(ServerIp, out _))
        {
            _errors[nameof(ServerIp)] = "Server IP must be a valid address.";
        }
        else
        {
            _errors.Remove(nameof(ServerIp));
        }

        if (string.IsNullOrWhiteSpace(LevelName))
        {
            _errors[nameof(LevelName)] = "Level name cannot be empty.";
        }
        else
        {
            _errors.Remove(nameof(LevelName));
        }

        if (!string.IsNullOrWhiteSpace(LevelSeed) && !long.TryParse(LevelSeed, out _))
        {
            _errors[nameof(LevelSeed)] = "Level seed should be numerical.";
        }
        else
        {
            _errors.Remove(nameof(LevelSeed));
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(string.Empty));
    }

    public IEnumerable GetErrors(string? propertyName)
    {
        return string.IsNullOrEmpty(propertyName)
            ? _errors.Values.SelectMany(error => error).ToArray()
            : _errors.TryGetValue(propertyName, out string? message)
                ? [message]
                : Array.Empty<string>();
    }
}
