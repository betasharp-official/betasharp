using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using BetaSharp.Launcher.Features.Authentication;
using BetaSharp.Launcher.Features.Sessions;
using BetaSharp.Launcher.Features.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BetaSharp.Launcher.Features.Home;

internal sealed partial class HomeViewModel : ObservableObject
{
    private static readonly IBrush s_statusStoppedBrush = new SolidColorBrush(Color.Parse("#9CA3AF"));
    private static readonly IBrush s_statusTransitionBrush = new SolidColorBrush(Color.Parse("#F59E0B"));
    private static readonly IBrush s_statusRunningBrush = new SolidColorBrush(Color.Parse("#22C55E"));
    private static readonly IBrush s_statusFailedBrush = new SolidColorBrush(Color.Parse("#EF4444"));
    private static readonly IBrush s_launchButtonBrush = new SolidColorBrush(Color.Parse("#16A34A"));
    private static readonly IBrush s_stopButtonBrush = new SolidColorBrush(Color.Parse("#DC2626"));
    private static readonly IBrush s_playIdleBrush = new SolidColorBrush(Color.Parse("#2563EB"));
    private static readonly IBrush s_playRunningBrush = new SolidColorBrush(Color.Parse("#16A34A"));
    private static readonly Geometry s_playIconGeometry = StreamGeometry.Parse("M2,1 L2,11 L10,6 Z");
    private static readonly Geometry s_stopIconGeometry = StreamGeometry.Parse("M2,2 H10 V10 H2 Z");

    private readonly NavigationService _navigationService;
    private readonly StorageService _storageService;
    private readonly ClientService _clientService;
    private readonly DedicatedServerService _dedicatedServerService;
    private readonly ServerPropertiesService _serverPropertiesService;

    private bool _updatingServerSettingsFields;
    private ServerLauncherSettings _lastStartedServerSettings;

    [ObservableProperty]
    public partial Session? Session { get; set; }

    [ObservableProperty]
    public partial bool IsClientRunning { get; set; }

    [ObservableProperty]
    public partial string PlayButtonText { get; set; } = "Play";

    [ObservableProperty]
    public partial IBrush PlayButtonBackground { get; set; } = s_playIdleBrush;

    [ObservableProperty]
    public partial bool IsPlayActionEnabled { get; set; } = true;

    [ObservableProperty]
    public partial double PlayButtonOpacity { get; set; } = 1.0;

    [ObservableProperty]
    public partial string DedicatedServerStatus { get; set; } = "Dedicated server is stopped.";

    [ObservableProperty]
    public partial string DedicatedServerStatusLabel { get; set; } = "Stopped";

    [ObservableProperty]
    public partial IBrush DedicatedServerStatusBrush { get; set; } = s_statusStoppedBrush;

    [ObservableProperty]
    public partial string DedicatedServerButtonText { get; set; } = "Launch Dedicated Server";

    [ObservableProperty]
    public partial Geometry DedicatedServerButtonIconData { get; set; } = s_playIconGeometry;

    [ObservableProperty]
    public partial IBrush DedicatedServerButtonBackground { get; set; } = s_launchButtonBrush;

    [ObservableProperty]
    public partial bool IsDedicatedServerActionEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsServerSettingsExpanded { get; set; }

    [ObservableProperty]
    public partial bool ServerOnlineMode { get; set; }

    [ObservableProperty]
    public partial string ServerPortText { get; set; } = ServerPropertiesService.DefaultPort.ToString();

    [ObservableProperty]
    public partial bool ServerAllowFlight { get; set; }

    [ObservableProperty]
    public partial string ServerMaxPlayersText { get; set; } = ServerPropertiesService.DefaultMaxPlayers.ToString();

    [ObservableProperty]
    public partial string ServerViewDistanceText { get; set; } = ServerPropertiesService.DefaultViewDistance.ToString();

    [ObservableProperty]
    public partial bool ServerSpawnMonsters { get; set; } = ServerPropertiesService.DefaultSpawnMonsters;

    [ObservableProperty]
    public partial bool HasServerSettingsValidationError { get; set; }

    [ObservableProperty]
    public partial string ServerSettingsValidationMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowServerSettingsRestartHint { get; set; }

    public HomeViewModel(
        NavigationService navigationService,
        StorageService storageService,
        ClientService clientService,
        DedicatedServerService dedicatedServerService,
        ServerPropertiesService serverPropertiesService)
    {
        _navigationService = navigationService;
        _storageService = storageService;
        _clientService = clientService;
        _dedicatedServerService = dedicatedServerService;
        _serverPropertiesService = serverPropertiesService;

        WeakReferenceMessenger.Default.Register<HomeViewModel, SessionMessage>(
            this,
            static (viewModel, message) => viewModel.Session = message.Session);

        WeakReference<HomeViewModel> weak = new(this);
        _dedicatedServerService.StatusChanged += (_, _) =>
        {
            if (weak.TryGetTarget(out HomeViewModel? viewModel))
            {
                viewModel.OnDedicatedServerStatusChanged();
            }
        };

        LoadServerSettings();
        UpdatePlayButtonState();
        UpdateDedicatedServerStatus();
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (IsClientRunning)
        {
            return;
        }

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

        try
        {
            IsClientRunning = true;
            UpdatePlayButtonState();

            using var process = Process.Start(info);
            ArgumentNullException.ThrowIfNull(process);

            await process.WaitForExitAsync();
        }
        finally
        {
            IsClientRunning = false;
            UpdatePlayButtonState();
        }
    }

    [RelayCommand]
    private async Task ToggleDedicatedServerAsync()
    {
        DedicatedServerState state = _dedicatedServerService.State;
        if (state is DedicatedServerState.Starting or DedicatedServerState.Stopping)
        {
            return;
        }

        if (state is DedicatedServerState.Stopped or DedicatedServerState.Failed)
        {
            if (Session?.HasExpired ?? true)
            {
                _navigationService.Navigate<AuthenticationViewModel>();
                return;
            }

            if (!TryBuildValidatedServerSettings(out ServerLauncherSettings settings, out string validationMessage))
            {
                SetServerValidationError(validationMessage);
                UpdateDedicatedServerStatus();
                return;
            }

            ClearServerValidationError();

            try
            {
                _serverPropertiesService.Save(settings);
            }
            catch (Exception exception)
            {
                SetServerValidationError($"Failed to save server settings: {exception.Message}");
                UpdateDedicatedServerStatus();
                return;
            }

            _lastStartedServerSettings = settings;

            await _dedicatedServerService.StartAsync();
            RefreshServerSettingsState();
            return;
        }

        await _dedicatedServerService.StopAsync();
        RefreshServerSettingsState();
    }

    [RelayCommand]
    private void SignOut()
    {
        _navigationService.Navigate<AuthenticationViewModel>();
        _storageService.Delete(nameof(Session));
    }

    partial void OnServerOnlineModeChanged(bool value) => OnServerSettingsEdited();
    partial void OnServerPortTextChanged(string value) => OnServerSettingsEdited();
    partial void OnServerAllowFlightChanged(bool value) => OnServerSettingsEdited();
    partial void OnServerMaxPlayersTextChanged(string value) => OnServerSettingsEdited();
    partial void OnServerViewDistanceTextChanged(string value) => OnServerSettingsEdited();
    partial void OnServerSpawnMonstersChanged(bool value) => OnServerSettingsEdited();

    private void OnServerSettingsEdited()
    {
        if (_updatingServerSettingsFields)
        {
            return;
        }

        RefreshServerSettingsState();
    }

    private void LoadServerSettings()
    {
        ServerLauncherSettings loaded = _serverPropertiesService.Load();
        _lastStartedServerSettings = loaded;

        _updatingServerSettingsFields = true;
        try
        {
            ServerOnlineMode = loaded.OnlineMode;
            ServerPortText = loaded.Port.ToString();
            ServerAllowFlight = loaded.AllowFlight;
            ServerMaxPlayersText = loaded.MaxPlayers.ToString();
            ServerViewDistanceText = loaded.ViewDistance.ToString();
            ServerSpawnMonsters = loaded.SpawnMonsters;
        }
        finally
        {
            _updatingServerSettingsFields = false;
        }

        RefreshServerSettingsState();
    }

    private void OnDedicatedServerStatusChanged()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            UpdateDedicatedServerStatus();
            RefreshServerSettingsState();
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            UpdateDedicatedServerStatus();
            RefreshServerSettingsState();
        });
    }

    private void UpdatePlayButtonState()
    {
        PlayButtonText = IsClientRunning ? "Running" : "Play";
        PlayButtonBackground = IsClientRunning ? s_playRunningBrush : s_playIdleBrush;
        IsPlayActionEnabled = !IsClientRunning;
        PlayButtonOpacity = IsClientRunning ? 0.7 : 1.0;
    }

    private void UpdateDedicatedServerStatus()
    {
        DedicatedServerStatus = _dedicatedServerService.Status;

        switch (_dedicatedServerService.State)
        {
            case DedicatedServerState.Starting:
                DedicatedServerStatusLabel = "Starting";
                DedicatedServerStatusBrush = s_statusTransitionBrush;
                DedicatedServerButtonText = "Starting Dedicated Server...";
                DedicatedServerButtonIconData = s_playIconGeometry;
                DedicatedServerButtonBackground = s_launchButtonBrush;
                IsDedicatedServerActionEnabled = false;
                break;
            case DedicatedServerState.Running:
                DedicatedServerStatusLabel = "Running";
                DedicatedServerStatusBrush = s_statusRunningBrush;
                DedicatedServerButtonText = "Stop Dedicated Server";
                DedicatedServerButtonIconData = s_stopIconGeometry;
                DedicatedServerButtonBackground = s_stopButtonBrush;
                IsDedicatedServerActionEnabled = true;
                break;
            case DedicatedServerState.Stopping:
                DedicatedServerStatusLabel = "Stopping";
                DedicatedServerStatusBrush = s_statusTransitionBrush;
                DedicatedServerButtonText = "Stopping Dedicated Server...";
                DedicatedServerButtonIconData = s_stopIconGeometry;
                DedicatedServerButtonBackground = s_stopButtonBrush;
                IsDedicatedServerActionEnabled = false;
                break;
            case DedicatedServerState.Failed:
                DedicatedServerStatusLabel = "Failed";
                DedicatedServerStatusBrush = s_statusFailedBrush;
                DedicatedServerButtonText = "Launch Dedicated Server";
                DedicatedServerButtonIconData = s_playIconGeometry;
                DedicatedServerButtonBackground = s_launchButtonBrush;
                IsDedicatedServerActionEnabled = !HasServerSettingsValidationError;
                break;
            default:
                DedicatedServerStatusLabel = "Stopped";
                DedicatedServerStatusBrush = s_statusStoppedBrush;
                DedicatedServerButtonText = "Launch Dedicated Server";
                DedicatedServerButtonIconData = s_playIconGeometry;
                DedicatedServerButtonBackground = s_launchButtonBrush;
                IsDedicatedServerActionEnabled = !HasServerSettingsValidationError;
                break;
        }
    }

    private void RefreshServerSettingsState()
    {
        bool valid = TryBuildValidatedServerSettings(out ServerLauncherSettings settings, out string validationMessage);

        if (valid)
        {
            ClearServerValidationError();
        }
        else
        {
            SetServerValidationError(validationMessage);
        }

        bool runningLike = IsDedicatedServerRunningLike();
        ShowServerSettingsRestartHint = runningLike;

        UpdateDedicatedServerStatus();
    }

    private bool TryBuildValidatedServerSettings(out ServerLauncherSettings settings, out string validationMessage)
    {
        var errors = new List<string>();

        if (!int.TryParse(ServerPortText, out int port) || port is < 1 or > 65535)
        {
            errors.Add("Server port must be a number between 1 and 65535.");
        }

        if (!int.TryParse(ServerMaxPlayersText, out int maxPlayers) || maxPlayers is < 1 or > 500)
        {
            errors.Add("Max players must be a number between 1 and 500.");
        }

        if (!int.TryParse(ServerViewDistanceText, out int viewDistance) || viewDistance is < 2 or > 32)
        {
            errors.Add("View distance must be a number between 2 and 32.");
        }

        if (errors.Count > 0)
        {
            settings = default;
            validationMessage = string.Join(Environment.NewLine, errors);
            return false;
        }

        settings = new ServerLauncherSettings(
            ServerOnlineMode,
            port,
            ServerAllowFlight,
            maxPlayers,
            viewDistance,
            ServerSpawnMonsters);
        validationMessage = string.Empty;
        return true;
    }

    private bool IsDedicatedServerRunningLike()
    {
        DedicatedServerState state = _dedicatedServerService.State;
        return state is DedicatedServerState.Starting or DedicatedServerState.Running or DedicatedServerState.Stopping;
    }

    private void ClearServerValidationError()
    {
        HasServerSettingsValidationError = false;
        ServerSettingsValidationMessage = string.Empty;
    }

    private void SetServerValidationError(string message)
    {
        HasServerSettingsValidationError = true;
        ServerSettingsValidationMessage = message;
    }
}
