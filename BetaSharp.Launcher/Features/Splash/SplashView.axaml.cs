using System;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Launcher.Features.Splash;

internal sealed partial class SplashView : UserControl
{
    private readonly ILogger<SplashView> _logger;
    private readonly SplashViewModel _viewModel;

    public SplashView(ILogger<SplashView> logger, SplashViewModel viewModel)
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
            _logger.LogError(exception, "An exception occurred in splash view on visual tree attach");
        }
    }
}
