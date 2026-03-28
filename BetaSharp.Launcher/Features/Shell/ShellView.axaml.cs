using System.ComponentModel;
using Avalonia.Controls;
using BetaSharp.Launcher.Features.Hosting;

namespace BetaSharp.Launcher.Features.Shell;

internal sealed partial class ShellView : Window
{
    private readonly HostingViewModel _hostingViewModel;

    public ShellView(ShellViewModel viewModel, HostingViewModel hostingViewModel)
    {
        _hostingViewModel = hostingViewModel;

        DataContext = viewModel;
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _hostingViewModel.Dispose();
        base.OnClosing(e);
    }
}
