using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BetaSharp.Launcher.Features.Authentication;
using BetaSharp.Launcher.Features.Hosting;
using BetaSharp.Launcher.Features.Sessions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Octokit;

namespace BetaSharp.Launcher.Features.Home;

internal sealed partial class HomeViewModel : ObservableObject
{
    public ObservableCollection<BetaSharpRelease> Releases { get; } = [];

    [ObservableProperty]
    public partial Session? Session { get; set; }

    private readonly ILogger<HomeViewModel> _logger;
    private readonly IGitHubClient _gitHubClient;
    private readonly NavigationService _navigationService;
    private readonly StorageService _storageService;
    private readonly ProcessService _processService;

    public HomeViewModel(
        ILogger<HomeViewModel> logger,
        IGitHubClient gitHubClient,
        NavigationService navigationService,
        StorageService storageService,
        ProcessService processService)
    {
        _logger = logger;
        _gitHubClient = gitHubClient;
        _navigationService = navigationService;
        _storageService = storageService;
        _processService = processService;

        // Replace messenger with a session store service?
        WeakReferenceMessenger.Default.Register<HomeViewModel, SessionMessage>(
            this,
            static (viewModel, message) => viewModel.Session = message.Session);
    }

    [RelayCommand]
    private void Initialize()
    {
        // Use observables?
        Task.Run(GetReleasesAsync);
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

    private async Task GetReleasesAsync()
    {
        try
        {
            var releases = await _gitHubClient.Repository.Release.GetAll("betasharp-official", nameof(BetaSharp));

            foreach (var release in releases)
            {
                Releases.Add(new BetaSharpRelease(
                    release.TagName,
                    (release.PublishedAt ?? release.CreatedAt).ToString("d"),
                    release.HtmlUrl));
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An exception occurred in home view-model on get releases");
        }
    }
}
