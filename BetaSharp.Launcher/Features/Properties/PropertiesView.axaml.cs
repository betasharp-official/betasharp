using System;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Launcher.Features.Properties;

internal sealed partial class PropertiesView : UserControl
{
    private readonly ILogger<PropertiesView> _logger;
    private readonly PropertiesViewModel _viewModel;

    public PropertiesView(ILogger<PropertiesView> logger, PropertiesViewModel viewModel)
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
            _logger.LogError(exception, "An exception occurred in properties view on visual tree attach");
        }
    }
}
