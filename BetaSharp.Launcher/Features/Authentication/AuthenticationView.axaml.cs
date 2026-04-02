using System;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Launcher.Features.Authentication;

internal sealed partial class AuthenticationView : UserControl
{
    private readonly ILogger<AuthenticationView> _logger;
    private readonly AuthenticationViewModel _viewModel;

    public AuthenticationView(ILogger<AuthenticationView> logger, AuthenticationViewModel viewModel)
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
            _logger.LogError(exception, "An exception occurred in authentication view on visual tree attach");
        }
    }
}
