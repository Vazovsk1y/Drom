using System.Windows.Controls;
using ClothesStore.WPF.Infrastructure;
using ClothesStore.WPF.ViewModels;

namespace ClothesStore.WPF.Views;

public partial class EditProductControl : UserControl, IDialogContent<EditProductViewModel>
{
    public EditProductControl(EditProductViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }

    public EditProductViewModel ViewModel { get; }
}