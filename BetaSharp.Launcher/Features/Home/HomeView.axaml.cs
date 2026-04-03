using Avalonia;
using Avalonia.Controls;

namespace BetaSharp.Launcher.Features.Home;

internal sealed partial class HomeView : UserControl
{
    private readonly HomeViewModel _viewModel;

    public HomeView(HomeViewModel viewModel)
    {
        _viewModel = viewModel;

        DataContext = viewModel;
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs eventArgs)
    {
        base.OnAttachedToVisualTree(eventArgs);
        _viewModel.InitializeCommand.Execute(null);
    }
}
