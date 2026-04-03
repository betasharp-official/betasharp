using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetaSharp.Launcher.Features.Shell;

internal sealed partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    public partial INotifyPropertyChanged? Current { get; set; }
}
