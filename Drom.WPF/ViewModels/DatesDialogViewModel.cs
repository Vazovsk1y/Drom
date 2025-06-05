using CommunityToolkit.Mvvm.ComponentModel;

namespace Drom.WPF.ViewModels;

public partial class DatesDialogViewModel : ObservableObject
{
    public const string DialogId = "DatesDialog";
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEnabled))]
    private DateTime? _from;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEnabled))]
    private DateTime? _to;

    public bool IsEnabled => From.HasValue && To.HasValue && (To.Value.Date > From.Value.Date);
}