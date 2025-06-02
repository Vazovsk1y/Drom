using System.Windows.Controls;
using ClothesStore.WPF.Infrastructure;
using ClothesStore.WPF.ViewModels;

namespace ClothesStore.WPF.Views;

public partial class OkCancelDialogControl : UserControl, IDialogContent<OkCancelDialogViewModel>
{
    public OkCancelDialogViewModel ViewModel { get; }

    public OkCancelDialogControl(OkCancelDialogViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }
}