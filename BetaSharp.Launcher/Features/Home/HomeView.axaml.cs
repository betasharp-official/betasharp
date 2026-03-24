using System;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Launcher.Features.Home;

internal sealed partial class HomeView : UserControl
{
    private readonly ILogger<HomeView> _logger;
    private readonly HomeViewModel _viewModel;

    public HomeView(ILogger<HomeView> logger, HomeViewModel viewModel)
    {
        _logger = logger;
        _viewModel = viewModel;

        DataContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs eventArgs)
    {
        try
        {
            base.OnAttachedToVisualTree(eventArgs);
            await _viewModel.InitializeCommand.ExecuteAsync(null);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An exception occurred in home view on visual tree attach");
        }
    }
}
