using CommunityToolkit.Mvvm.ComponentModel;

namespace ClothesStore.WPF.ViewModels;

public partial class OkCancelDialogViewModel : ObservableObject
{
    public const string DialogId = "OkCancelDialog";
    
    [ObservableProperty]
    private string? _message;
}